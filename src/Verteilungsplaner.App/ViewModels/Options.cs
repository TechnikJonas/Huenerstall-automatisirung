using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Auswahlwert mit deutschem Anzeigenamen (für Auswahlfelder).</summary>
public record Option<T>(T Value, string Name);

public static class Options
{
    public static IReadOnlyList<Option<ActuatorKind>> ActuatorKinds { get; } = new[]
    {
        new Option<ActuatorKind>(ActuatorKind.Switch, "Schaltaktor"),
        new Option<ActuatorKind>(ActuatorKind.Dimmer, "Dimmaktor"),
    };

    public static IReadOnlyList<Option<ActuatorMounting>> ActuatorMountings { get; } = new[]
    {
        new Option<ActuatorMounting>(ActuatorMounting.DistributionBoard, "Verteilung (REG)"),
        new Option<ActuatorMounting>(ActuatorMounting.Room, "Raum (UP-Dose)"),
    };

    public static string KindName(ActuatorKind kind) => ActuatorKinds.First(o => o.Value == kind).Name;

    public static string MountingName(ActuatorMounting mounting) => ActuatorMountings.First(o => o.Value == mounting).Name;
}
