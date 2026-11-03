namespace Verteilungsplaner.Core.Models;

/// <summary>
/// Funktion eines Kabels (z. B. Beleuchtung, Steckdosen, Rollladen).
/// Funktionen werden in den Einstellungen gepflegt und gelten für alle Kabellisten.
/// </summary>
public class CableFunction : ObservableEntity
{
    private string _name = string.Empty;
    private string? _description;
    private int _sortOrder;

    public string Name { get => _name; set => SetField(ref _name, value); }

    public string? Description { get => _description; set => SetField(ref _description, value); }

    public int SortOrder { get => _sortOrder; set => SetField(ref _sortOrder, value); }
}
