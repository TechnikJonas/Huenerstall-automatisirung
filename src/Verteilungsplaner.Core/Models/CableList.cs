namespace Verteilungsplaner.Core.Models;

/// <summary>Eine Kabelliste, die zu einem Projekt und einer UV gehört.</summary>
public class CableList : ObservableEntity
{
    private string _name = string.Empty;
    private int _distributionBoardId;
    private string? _remark;

    public int ProjectId { get; set; }

    public int DistributionBoardId { get => _distributionBoardId; set => SetField(ref _distributionBoardId, value); }

    public string Name { get => _name; set => SetField(ref _name, value); }

    public string? Remark { get => _remark; set => SetField(ref _remark, value); }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
