using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Tabellenzeile für einen Aktor im Projekt.</summary>
public partial class ActuatorRow : ObservableObject
{
    private readonly ActuatorsViewModel _owner;

    public ActuatorRow(Actuator entity, ActuatorsViewModel owner)
    {
        Entity = entity;
        _owner = owner;
        LocationOptions = BuildLocationOptions();
        Entity.PropertyChanged += OnEntityChanged;
    }

    public Actuator Entity { get; }

    public ActuatorTemplate? Template => _owner.FindTemplate(Entity.TemplateId);

    public string TemplateInfo
    {
        get
        {
            if (Template is not { } t)
                return "–";
            var limits = t.MaxPowerPerChannel is double w && w > 0
                ? $"{t.MaxCurrentPerChannel:0.##} A / {w:0.##} W"
                : $"{t.MaxCurrentPerChannel:0.##} A";
            return $"{Options.KindName(t.Kind)} · {t.ChannelCount} Kanäle · {limits} je Kanal";
        }
    }

    public string MountingName => Template is { } t ? Options.MountingName(t.Mounting) : "–";

    [ObservableProperty]
    private IReadOnlyList<LookupItem> _locationOptions = Array.Empty<LookupItem>();

    [ObservableProperty]
    private string _occupancy = string.Empty;

    /// <summary>UV bzw. Raum – je nachdem, wo die Vorlage eingebaut wird.</summary>
    public int? LocationId
    {
        get => Template?.Mounting == ActuatorMounting.Room ? Entity.RoomId : Entity.DistributionBoardId;
        set
        {
            if (value == LocationId)
                return;

            if (Template?.Mounting == ActuatorMounting.Room)
            {
                Entity.RoomId = value;
                Entity.DistributionBoardId = null;
            }
            else
            {
                Entity.DistributionBoardId = value;
                Entity.RoomId = null;
            }
            OnPropertyChanged();
        }
    }

    public void Detach() => Entity.PropertyChanged -= OnEntityChanged;

    private void OnEntityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Actuator.TemplateId))
            return;

        OnPropertyChanged(nameof(Template));
        OnPropertyChanged(nameof(TemplateInfo));
        OnPropertyChanged(nameof(MountingName));

        // Erst die Auswahl tauschen, dann einen passenden Einbauort vorbelegen.
        LocationOptions = BuildLocationOptions();
        if (LocationId == null || LocationOptions.All(o => o.Id != LocationId))
            LocationId = LocationOptions.FirstOrDefault(o => o.Id != null)?.Id;
        OnPropertyChanged(nameof(LocationId));
        _owner.RefreshUsage();
    }

    private IReadOnlyList<LookupItem> BuildLocationOptions() =>
        Template?.Mounting == ActuatorMounting.Room ? _owner.RoomOptions : _owner.BoardOptions;
}
