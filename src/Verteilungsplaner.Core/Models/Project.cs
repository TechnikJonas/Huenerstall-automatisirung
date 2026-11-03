namespace Verteilungsplaner.Core.Models;

/// <summary>Ein Bauvorhaben / Projekt, z. B. "EFH Müller".</summary>
public class Project : ObservableEntity
{
    private string _name = string.Empty;
    private string? _number;
    private string? _customer;
    private string? _address;
    private string? _description;

    public string Name { get => _name; set => SetField(ref _name, value); }

    /// <summary>Projekt- bzw. Auftragsnummer.</summary>
    public string? Number { get => _number; set => SetField(ref _number, value); }

    public string? Customer { get => _customer; set => SetField(ref _customer, value); }

    public string? Address { get => _address; set => SetField(ref _address, value); }

    public string? Description { get => _description; set => SetField(ref _description, value); }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
