namespace Verteilungsplaner.Core.Models;

public enum ActuatorKind
{
    /// <summary>Schaltaktor</summary>
    Switch = 0,

    /// <summary>Dimmaktor</summary>
    Dimmer = 1,
}

public enum ActuatorMounting
{
    /// <summary>Reiheneinbau in der Verteilung (REG).</summary>
    DistributionBoard = 0,

    /// <summary>Unterputz im Raum (UP-Dose).</summary>
    Room = 1,
}

/// <summary>
/// Grundkonfiguration eines Aktors (z. B. "Schaltaktor 8-fach 16 A").
/// Vorlagen gelten für alle Projekte und werden in den Einstellungen gepflegt.
/// </summary>
public class ActuatorTemplate : ObservableEntity
{
    private string _name = string.Empty;
    private ActuatorKind _kind;
    private ActuatorMounting _mounting;
    private int _channelCount = 1;
    private double _maxCurrentPerChannel;
    private double? _maxPowerPerChannel;
    private string? _manufacturer;
    private string? _articleNumber;
    private double? _widthTe;

    public string Name { get => _name; set => SetField(ref _name, value); }

    public ActuatorKind Kind { get => _kind; set => SetField(ref _kind, value); }

    /// <summary>Einbauort: Verteilung oder Raum (UP-Dose).</summary>
    public ActuatorMounting Mounting { get => _mounting; set => SetField(ref _mounting, value); }

    public int ChannelCount { get => _channelCount; set => SetField(ref _channelCount, Math.Max(1, value)); }

    /// <summary>Maximale Strombelastbarkeit je Kanal in A.</summary>
    public double MaxCurrentPerChannel { get => _maxCurrentPerChannel; set => SetField(ref _maxCurrentPerChannel, Math.Max(0, value)); }

    /// <summary>Maximale Leistung je Kanal in W (vor allem für Dimmaktoren).</summary>
    public double? MaxPowerPerChannel { get => _maxPowerPerChannel; set => SetField(ref _maxPowerPerChannel, value); }

    public string? Manufacturer { get => _manufacturer; set => SetField(ref _manufacturer, value); }

    public string? ArticleNumber { get => _articleNumber; set => SetField(ref _articleNumber, value); }

    /// <summary>Breite in Teilungseinheiten (nur bei Einbau in der Verteilung relevant).</summary>
    public double? WidthTe { get => _widthTe; set => SetField(ref _widthTe, value); }
}
