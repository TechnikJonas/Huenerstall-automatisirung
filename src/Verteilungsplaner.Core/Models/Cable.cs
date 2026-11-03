namespace Verteilungsplaner.Core.Models;

/// <summary>Ein einzelnes Kabel (eine Zeile) in einer Kabelliste.</summary>
public class Cable : ObservableEntity
{
    private int _sortOrder;
    private string _number = string.Empty;
    private string? _designation;
    private int? _roomId;
    private int? _functionId;
    private string? _cableType;
    private string? _crossSection;
    private double? _length;
    private string? _fuse;
    private string? _terminal;
    private string? _remark;
    private string? _additionalText;

    public int CableListId { get; set; }

    public int SortOrder { get => _sortOrder; set => SetField(ref _sortOrder, value); }

    /// <summary>Kabel-Nr., z. B. "W1".</summary>
    public string Number { get => _number; set => SetField(ref _number, value); }

    /// <summary>Bezeichnung, z. B. "Deckenlicht Wohnzimmer".</summary>
    public string? Designation { get => _designation; set => SetField(ref _designation, value); }

    public int? RoomId { get => _roomId; set => SetField(ref _roomId, value); }

    public int? FunctionId { get => _functionId; set => SetField(ref _functionId, value); }

    /// <summary>Kabeltyp, z. B. "NYM-J".</summary>
    public string? CableType { get => _cableType; set => SetField(ref _cableType, value); }

    /// <summary>Adern × Querschnitt, z. B. "3x1,5".</summary>
    public string? CrossSection { get => _crossSection; set => SetField(ref _crossSection, value); }

    /// <summary>Länge in Metern.</summary>
    public double? Length { get => _length; set => SetField(ref _length, value); }

    /// <summary>Absicherung, z. B. "B16".</summary>
    public string? Fuse { get => _fuse; set => SetField(ref _fuse, value); }

    /// <summary>Klemmstelle in der UV, z. B. "X1:1-3".</summary>
    public string? Terminal { get => _terminal; set => SetField(ref _terminal, value); }

    public string? Remark { get => _remark; set => SetField(ref _remark, value); }

    public string? AdditionalText { get => _additionalText; set => SetField(ref _additionalText, value); }
}
