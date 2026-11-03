using System.Globalization;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.Core.Services;

public enum LoadState
{
    /// <summary>Kein Aktor zugewiesen.</summary>
    Unassigned,
    Ok,
    Warning,
    Overload,
}

/// <summary>Belegung eines Aktorkanals.</summary>
public record ChannelUsage(
    int Channel,
    IReadOnlyList<RoomFunction> Assigned,
    double TotalCurrent,
    double TotalPower,
    LoadState State,
    string Status);

/// <summary>Ergebnis der Prüfung für eine Raum-Funktion.</summary>
public record RoomFunctionCheck(LoadState State, string Status);

/// <summary>
/// Prüft die Belegung von Aktorkanälen: Kanal vorhanden, Strom (A) und Leistung (W)
/// je Kanal innerhalb der Grenzwerte der Aktor-Vorlage.
/// Mehrere Raum-Funktionen auf demselben Kanal werden zusammengezählt.
/// </summary>
public static class ActuatorLoadCheck
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static IReadOnlyList<ChannelUsage> GetChannelUsage(
        Actuator actuator, ActuatorTemplate template, IEnumerable<RoomFunction> roomFunctions)
    {
        var assigned = roomFunctions.Where(r => r.ActuatorId == actuator.Id).ToList();
        var result = new List<ChannelUsage>();

        for (var ch = 1; ch <= template.ChannelCount; ch++)
        {
            var onChannel = assigned.Where(r => r.Channel == ch).ToList();
            result.Add(EvaluateChannel(ch, onChannel, template));
        }

        return result;
    }

    public static RoomFunctionCheck Check(
        RoomFunction roomFunction,
        IReadOnlyDictionary<int, Actuator> actuators,
        IReadOnlyDictionary<int, ActuatorTemplate> templates,
        IEnumerable<RoomFunction> allRoomFunctions)
    {
        if (roomFunction.ActuatorId is not int actuatorId || !actuators.TryGetValue(actuatorId, out var actuator))
            return new RoomFunctionCheck(LoadState.Unassigned, "nicht zugewiesen");

        if (!templates.TryGetValue(actuator.TemplateId, out var template))
            return new RoomFunctionCheck(LoadState.Warning, "⚠ Aktor-Vorlage fehlt");

        if (roomFunction.Channel is not int channel)
            return new RoomFunctionCheck(LoadState.Warning, "⚠ Kanal fehlt");

        if (channel < 1 || channel > template.ChannelCount)
            return new RoomFunctionCheck(LoadState.Warning, $"⚠ Kanal {channel} gibt es nicht (1–{template.ChannelCount})");

        var onChannel = allRoomFunctions.Where(r => r.ActuatorId == actuatorId && r.Channel == channel).ToList();
        var usage = EvaluateChannel(channel, onChannel, template);

        if (usage.State == LoadState.Ok
            && template.Mounting == ActuatorMounting.Room
            && actuator.RoomId != null
            && actuator.RoomId != roomFunction.RoomId)
        {
            return new RoomFunctionCheck(LoadState.Warning, "⚠ UP-Aktor sitzt in anderem Raum · " + usage.Status);
        }

        return new RoomFunctionCheck(usage.State, usage.Status);
    }

    private static ChannelUsage EvaluateChannel(int channel, IReadOnlyList<RoomFunction> onChannel, ActuatorTemplate template)
    {
        var current = onChannel.Sum(r => r.LoadCurrent ?? 0);
        var power = onChannel.Sum(r => r.LoadPower ?? 0);

        if (onChannel.Count == 0)
            return new ChannelUsage(channel, onChannel, 0, 0, LoadState.Unassigned, "frei");

        var problems = new List<string>();
        if (template.MaxCurrentPerChannel > 0 && current > template.MaxCurrentPerChannel + 1e-9)
            problems.Add($"{F(current)} A > {F(template.MaxCurrentPerChannel)} A");
        if (template.MaxPowerPerChannel is double maxW && maxW > 0 && power > maxW + 1e-9)
            problems.Add($"{F(power)} W > {F(maxW)} W");

        if (problems.Count > 0)
            return new ChannelUsage(channel, onChannel, current, power, LoadState.Overload, "⛔ Überlast: " + string.Join(", ", problems));

        var parts = new List<string>();
        if (template.MaxCurrentPerChannel > 0)
            parts.Add($"{F(current)} / {F(template.MaxCurrentPerChannel)} A");
        if (template.MaxPowerPerChannel is double w && w > 0)
            parts.Add($"{F(power)} / {F(w)} W");

        var status = "✓ OK" + (parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty);
        if (onChannel.All(r => r.LoadCurrent == null && r.LoadPower == null))
            return new ChannelUsage(channel, onChannel, current, power, LoadState.Warning, "⚠ Keine Last eingetragen");

        return new ChannelUsage(channel, onChannel, current, power, LoadState.Ok, status);
    }

    private static string F(double value) => value.ToString("0.##", De);
}
