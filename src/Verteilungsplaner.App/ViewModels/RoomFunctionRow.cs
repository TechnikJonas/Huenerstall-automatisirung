using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Tabellenzeile "Raum – Funktion – Kreis" mit Aktorzuweisung.</summary>
public partial class RoomFunctionRow : ObservableObject
{
    private readonly RoomFunctionsViewModel _owner;

    public RoomFunctionRow(RoomFunction entity, RoomFunctionsViewModel owner, string roomLabel, string functionName, int functionOrder, string cables)
    {
        Entity = entity;
        _owner = owner;
        RoomLabel = roomLabel;
        FunctionName = functionName;
        FunctionOrder = functionOrder;
        Cables = cables;
        ChannelOptions = owner.BuildChannelOptions(entity.ActuatorId);
        Entity.PropertyChanged += OnEntityChanged;
    }

    public RoomFunction Entity { get; }

    public string RoomLabel { get; }

    public string FunctionName { get; }

    public int FunctionOrder { get; }

    /// <summary>Zugehörige Kabel aus den Kabellisten, z. B. "W1, W4 (UV-EG)".</summary>
    public string Cables { get; }

    [ObservableProperty]
    private IReadOnlyList<LookupItem> _channelOptions = Array.Empty<LookupItem>();

    [ObservableProperty]
    private LoadState _state = LoadState.Unassigned;

    [ObservableProperty]
    private string _status = string.Empty;

    public void Detach() => Entity.PropertyChanged -= OnEntityChanged;

    private void OnEntityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RoomFunction.ActuatorId))
        {
            // Erst die Kanalauswahl tauschen, dann einen freien Kanal vorschlagen.
            ChannelOptions = _owner.BuildChannelOptions(Entity.ActuatorId);
            if (Entity.ActuatorId == null)
                Entity.Channel = null;
            else if (Entity.Channel == null || ChannelOptions.All(o => o.Id != Entity.Channel))
                Entity.Channel = _owner.FindFreeChannel(Entity.ActuatorId.Value, Entity);
        }
    }
}
