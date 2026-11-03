using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Zeile der Kanalübersicht eines Aktors.</summary>
public record ChannelRow(int Channel, string Assigned, string Current, string Power, LoadState State, string Status);

/// <summary>Aktoren eines Projekts anlegen (aus Vorlagen) und Kanalbelegung ansehen.</summary>
public partial class ActuatorsViewModel : ObservableObject, IProjectPage
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly AutoSaver _autoSaver;
    private Dictionary<int, ActuatorTemplate> _templates = new();
    private List<RoomFunction> _roomFunctions = new();
    private Dictionary<int, string> _roomNames = new();
    private Dictionary<int, string> _functionNames = new();

    public ActuatorsViewModel(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
        _autoSaver = new AutoSaver(data, dialogs);
    }

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<ActuatorTemplate> Templates { get; } = new();
    public ObservableCollection<ActuatorRow> Actuators { get; } = new();
    public ObservableCollection<ChannelRow> Channels { get; } = new();

    public IReadOnlyList<LookupItem> BoardOptions { get; private set; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> RoomOptions { get; private set; } = Array.Empty<LookupItem>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProject))]
    [NotifyCanExecuteChangedFor(nameof(AddActuatorCommand))]
    private Project? _selectedProject;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteActuatorCommand))]
    private ActuatorRow? _selectedActuator;

    [ObservableProperty]
    private ActuatorTemplate? _newTemplate;

    [ObservableProperty]
    private string _channelTitle = "Kanalbelegung";

    [ObservableProperty]
    private string _summary = string.Empty;

    public bool HasProject => SelectedProject != null;

    public ActuatorTemplate? FindTemplate(int id) => _templates.GetValueOrDefault(id);

    public void Load(int? projectId)
    {
        var keep = projectId ?? SelectedProject?.Id;

        // Erst Projekt abwählen (leert die Tabelle), dann die Auswahllisten neu laden.
        SelectedProject = null;
        _templates = _data.GetActuatorTemplates().ToDictionary(t => t.Id);
        Templates.Clear();
        foreach (var t in _templates.Values)
            Templates.Add(t);
        NewTemplate ??= Templates.FirstOrDefault();
        if (NewTemplate != null)
            NewTemplate = Templates.FirstOrDefault(t => t.Id == NewTemplate.Id) ?? Templates.FirstOrDefault();

        Projects.Clear();
        foreach (var p in _data.GetProjects())
            Projects.Add(p);
        SelectedProject = Projects.FirstOrDefault(p => p.Id == keep) ?? Projects.FirstOrDefault();
    }

    partial void OnSelectedProjectChanged(Project? value)
    {
        ClearActuators();
        BoardOptions = Array.Empty<LookupItem>();
        RoomOptions = Array.Empty<LookupItem>();
        _roomFunctions = new();
        if (value == null)
        {
            RefreshUsage();
            return;
        }

        var rooms = _data.GetRooms(value.Id);
        _roomNames = rooms.ToDictionary(r => r.Id, r => string.IsNullOrWhiteSpace(r.Floor) ? r.Name : $"{r.Floor} – {r.Name}");
        _functionNames = _data.GetFunctions().ToDictionary(f => f.Id, f => f.Name);
        BoardOptions = new[] { LookupItem.None }
            .Concat(_data.GetDistributionBoards(value.Id).Select(b => new LookupItem(b.Id, b.Name))).ToList();
        RoomOptions = new[] { LookupItem.None }
            .Concat(rooms.Select(r => new LookupItem(r.Id, _roomNames[r.Id]))).ToList();
        _roomFunctions = _data.GetRoomFunctions(value.Id);

        foreach (var a in _data.GetActuators(value.Id))
        {
            _autoSaver.Track(a);
            Actuators.Add(new ActuatorRow(a, this));
        }

        SelectedActuator = Actuators.FirstOrDefault();
        RefreshUsage();
    }

    private void ClearActuators()
    {
        _autoSaver.UntrackAll<Actuator>();
        foreach (var row in Actuators)
            row.Detach();
        Actuators.Clear();
    }

    partial void OnSelectedActuatorChanged(ActuatorRow? value) => RefreshChannels();

    /// <summary>Aktualisiert Belegung ("3/8 belegt") und Kanalübersicht.</summary>
    public void RefreshUsage()
    {
        foreach (var row in Actuators)
        {
            if (row.Template is not { } t)
            {
                row.Occupancy = "–";
                continue;
            }

            var usage = ActuatorLoadCheck.GetChannelUsage(row.Entity, t, _roomFunctions);
            var used = usage.Count(u => u.Assigned.Count > 0);
            var overload = usage.Count(u => u.State == LoadState.Overload);
            row.Occupancy = $"{used}/{t.ChannelCount} belegt" + (overload > 0 ? $" · ⛔ {overload} Überlast" : string.Empty);
        }

        var totalChannels = Actuators.Sum(a => a.Template?.ChannelCount ?? 0);
        var usedChannels = Actuators.Sum(a => a.Template is { } t
            ? ActuatorLoadCheck.GetChannelUsage(a.Entity, t, _roomFunctions).Count(u => u.Assigned.Count > 0)
            : 0);
        Summary = $"{Actuators.Count} Aktor(en)  ·  {usedChannels} von {totalChannels} Kanälen belegt  ·  {totalChannels - usedChannels} frei";

        RefreshChannels();
    }

    private void RefreshChannels()
    {
        Channels.Clear();
        if (SelectedActuator is not { Template: { } t } row)
        {
            ChannelTitle = "Kanalbelegung";
            return;
        }

        ChannelTitle = $"Kanalbelegung {row.Entity.Name} – {t.Name}";
        foreach (var u in ActuatorLoadCheck.GetChannelUsage(row.Entity, t, _roomFunctions))
        {
            var assigned = string.Join(", ", u.Assigned.Select(Label));
            Channels.Add(new ChannelRow(
                u.Channel,
                assigned.Length == 0 ? "–" : assigned,
                u.Assigned.Count == 0 ? "" : $"{u.TotalCurrent:0.##} A",
                u.Assigned.Count == 0 || u.TotalPower == 0 ? "" : $"{u.TotalPower:0.##} W",
                u.State,
                u.Status));
        }
    }

    private string Label(RoomFunction rf)
    {
        var text = $"{_roomNames.GetValueOrDefault(rf.RoomId, "?")} – {_functionNames.GetValueOrDefault(rf.FunctionId, "?")}";
        if (rf.Circuit > 1 || !string.IsNullOrWhiteSpace(rf.Designation))
            text += $" (Kreis {rf.Circuit}{(string.IsNullOrWhiteSpace(rf.Designation) ? "" : ": " + rf.Designation)})";
        return text;
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddActuator()
    {
        if (SelectedProject == null)
            return;

        if (NewTemplate == null)
        {
            _dialogs.Info("Bitte zuerst eine Aktor-Vorlage auswählen.\nVorlagen werden unter Einstellungen → Aktor-Vorlagen gepflegt.");
            return;
        }

        var actuator = new Actuator
        {
            ProjectId = SelectedProject.Id,
            TemplateId = NewTemplate.Id,
            Name = _data.SuggestNextActuatorName(SelectedProject.Id),
        };
        if (NewTemplate.Mounting == ActuatorMounting.DistributionBoard)
            actuator.DistributionBoardId = BoardOptions.FirstOrDefault(o => o.Id != null)?.Id;

        _data.Add(actuator);
        _autoSaver.Track(actuator);
        var row = new ActuatorRow(actuator, this);
        Actuators.Add(row);
        SelectedActuator = row;
        RefreshUsage();
    }

    private bool HasActuator() => SelectedActuator != null;

    [RelayCommand(CanExecute = nameof(HasActuator))]
    private void DeleteActuator()
    {
        if (SelectedActuator is not { } row)
            return;

        var used = _data.CountRoomFunctionsOfActuator(row.Entity.Id);
        var message = $"Aktor \"{row.Entity.Name}\" wirklich löschen?";
        if (used > 0)
            message += $"\n\n{used} Raum-Funktion(en) sind ihm zugewiesen. Diese Zuordnungen werden entfernt.";
        if (!_dialogs.Confirm(message))
            return;

        _data.DeleteActuator(row.Entity);
        _autoSaver.Untrack(row.Entity);
        row.Detach();
        Actuators.Remove(row);
        _roomFunctions = SelectedProject == null ? new() : _data.GetRoomFunctions(SelectedProject.Id);
        SelectedActuator = Actuators.FirstOrDefault();
        RefreshUsage();
    }
}
