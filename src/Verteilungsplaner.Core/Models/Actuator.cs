namespace Verteilungsplaner.Core.Models;

/// <summary>
/// Ein im Projekt eingesetzter Aktor, erzeugt aus einer <see cref="ActuatorTemplate"/>.
/// Sitzt je nach Vorlage in einer UV oder in einem Raum (UP-Dose).
/// </summary>
public class Actuator : ObservableEntity
{
    private string _name = string.Empty;
    private int _templateId;
    private int? _distributionBoardId;
    private int? _roomId;
    private string? _remark;

    public int ProjectId { get; set; }

    public int TemplateId { get => _templateId; set => SetField(ref _templateId, value); }

    /// <summary>Kennzeichnung, z. B. "A1".</summary>
    public string Name { get => _name; set => SetField(ref _name, value); }

    /// <summary>UV, in der der Aktor sitzt (bei Vorlagen für die Verteilung).</summary>
    public int? DistributionBoardId { get => _distributionBoardId; set => SetField(ref _distributionBoardId, value); }

    /// <summary>Raum, in dem der Aktor sitzt (bei UP-Aktoren).</summary>
    public int? RoomId { get => _roomId; set => SetField(ref _roomId, value); }

    public string? Remark { get => _remark; set => SetField(ref _remark, value); }
}
