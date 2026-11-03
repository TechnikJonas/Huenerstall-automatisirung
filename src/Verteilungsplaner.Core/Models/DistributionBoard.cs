namespace Verteilungsplaner.Core.Models;

/// <summary>Eine Unterverteilung (UV) innerhalb eines Projekts.</summary>
public class DistributionBoard : ObservableEntity
{
    private string _name = string.Empty;
    private string? _location;
    private string? _description;

    public int ProjectId { get; set; }

    /// <summary>Bezeichnung, z. B. "UV-EG".</summary>
    public string Name { get => _name; set => SetField(ref _name, value); }

    /// <summary>Einbauort, z. B. "Flur EG".</summary>
    public string? Location { get => _location; set => SetField(ref _location, value); }

    public string? Description { get => _description; set => SetField(ref _description, value); }
}
