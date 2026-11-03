namespace Verteilungsplaner.Core.Models;

/// <summary>Ein Raum innerhalb eines Projekts.</summary>
public class Room : ObservableEntity
{
    private string _name = string.Empty;
    private string? _floor;

    public int ProjectId { get; set; }

    public string Name { get => _name; set => SetField(ref _name, value); }

    /// <summary>Geschoss, z. B. "EG", "OG", "KG".</summary>
    public string? Floor { get => _floor; set => SetField(ref _floor, value); }
}
