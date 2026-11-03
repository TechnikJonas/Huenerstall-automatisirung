namespace Verteilungsplaner.Core.Models;

/// <summary>
/// Eine Funktion in einem Raum (z. B. "Küche – Beleuchtung, Kreis 1").
/// Gleiche Raum-Funktionen können in mehrere Kreise aufgeteilt werden.
/// Jeder Kreis kann einem Kanal eines Schalt- oder Dimmaktors zugewiesen werden.
/// </summary>
public class RoomFunction : ObservableEntity
{
    private int _roomId;
    private int _functionId;
    private int _circuit = 1;
    private string? _designation;
    private double? _loadCurrent;
    private double? _loadPower;
    private int? _actuatorId;
    private int? _channel;
    private string? _remark;

    public int ProjectId { get; set; }

    public int RoomId { get => _roomId; set => SetField(ref _roomId, value); }

    public int FunctionId { get => _functionId; set => SetField(ref _functionId, value); }

    /// <summary>Kreis-Nummer innerhalb gleicher Raum-Funktion (1, 2, 3, …).</summary>
    public int Circuit { get => _circuit; set => SetField(ref _circuit, Math.Max(1, value)); }

    /// <summary>Optionale Bezeichnung, z. B. "Deckenspots".</summary>
    public string? Designation { get => _designation; set => SetField(ref _designation, value); }

    /// <summary>Last in A.</summary>
    public double? LoadCurrent { get => _loadCurrent; set => SetField(ref _loadCurrent, value); }

    /// <summary>Last in W.</summary>
    public double? LoadPower { get => _loadPower; set => SetField(ref _loadPower, value); }

    public int? ActuatorId { get => _actuatorId; set => SetField(ref _actuatorId, value); }

    /// <summary>Kanal am Aktor (1 … Kanalanzahl).</summary>
    public int? Channel { get => _channel; set => SetField(ref _channel, value); }

    public string? Remark { get => _remark; set => SetField(ref _remark, value); }
}
