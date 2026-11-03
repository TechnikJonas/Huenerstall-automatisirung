using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>
/// Tabelle aller Funktionen je Raum (aus den Kabellisten) mit Aufteilung in Kreise
/// und Zuweisung zu Schalt- bzw. Dimmaktorkanälen inkl. Lastprüfung.
/// </summary>
public partial class RoomFunctionsViewModel : ObservableObject, IProjectPage
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly AutoSaver _autoSaver;

    private Dictionary<int, Actuator> _actuators = new();
    private Dictionary<int, ActuatorTemplate> _templates = new();
    private Dictionary<int, Room> _rooms = new();
    private Dictionary<int, CableFunction> _functions = new();

    public RoomFunctionsViewModel(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
        _autoSaver = new AutoSaver(data, dialogs);
        _autoSaver.Saved += (_, e) =>
        {
            if (e is RoomFunction)
                RefreshStatus();
        };

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = FilterRow;
    }

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<RoomFunctionRow> Rows { get; } = new();
    public ICollectionView RowsView { get; }

    /// <summary>Auswahl der Aktoren des Projekts (inkl. "—").</summary>
    public ObservableCollection<LookupItem> ActuatorOptions { get; } = new();

    /// <summary>Raumauswahl für Filter und neue Zeilen.</summary>
    public ObservableCollection<LookupItem> RoomFilterOptions { get; } = new();
    public ObservableCollection<LookupItem> RoomOptions { get; } = new();
    public ObservableCollection<LookupItem> FunctionOptions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProject))]
    [NotifyCanExecuteChangedFor(nameof(AddRoomFunctionCommand), nameof(SyncFromCablesCommand))]
    private Project? _selectedProject;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCircuitCommand), nameof(DeleteRowCommand))]
    private RoomFunctionRow? _selectedRow;

    [ObservableProperty]
    private int? _roomFilter;

    [ObservableProperty]
    private bool _onlyUnassigned;

    [ObservableProperty]
    private int? _newRoomId;

    [ObservableProperty]
    private int? _newFunctionId;

    [ObservableProperty]
    private string _summary = string.Empty;

    public bool HasProject => SelectedProject != null;

    partial void OnRoomFilterChanged(int? value) => RefreshFilter();

    partial void OnOnlyUnassignedChanged(bool value) => RefreshFilter();

    private bool FilterRow(object item) =>
        item is RoomFunctionRow row
        && (RoomFilter == null || row.Entity.RoomId == RoomFilter)
        && (!OnlyUnassigned || row.Entity.ActuatorId == null);

    public void Load(int? projectId)
    {
        var keep = projectId ?? SelectedProject?.Id;
        SelectedProject = null;
        Projects.Clear();
        foreach (var p in _data.GetProjects())
            Projects.Add(p);
        SelectedProject = Projects.FirstOrDefault(p => p.Id == keep) ?? Projects.FirstOrDefault();
    }

    partial void OnSelectedProjectChanged(Project? value)
    {
        // Reihenfolge wichtig: erst Zeilen leeren, dann Auswahllisten.
        ClearRows();
        ActuatorOptions.Clear();
        RoomOptions.Clear();
        RoomFilterOptions.Clear();
        FunctionOptions.Clear();
        RoomFilter = null;

        if (value == null)
        {
            Summary = string.Empty;
            return;
        }

        // Neue Raum/Funktion-Kombinationen aus den Kabellisten automatisch übernehmen
        _data.SyncRoomFunctionsFromCables(value.Id);

        _rooms = _data.GetRooms(value.Id).ToDictionary(r => r.Id);
        _functions = _data.GetFunctions().ToDictionary(f => f.Id);
        _templates = _data.GetActuatorTemplates().ToDictionary(t => t.Id);
        _actuators = _data.GetActuators(value.Id).ToDictionary(a => a.Id);
        var boards = _data.GetDistributionBoards(value.Id).ToDictionary(b => b.Id);

        ActuatorOptions.Add(LookupItem.None);
        foreach (var a in _actuators.Values.OrderBy(a => a.Name))
        {
            var t = _templates.GetValueOrDefault(a.TemplateId);
            var place = a.DistributionBoardId is int b && boards.TryGetValue(b, out var board) ? board.Name
                : a.RoomId is int r && _rooms.TryGetValue(r, out var room) ? room.Name
                : null;
            var label = $"{a.Name} – {t?.Name ?? "?"}" + (place == null ? "" : $" ({place})");
            ActuatorOptions.Add(new LookupItem(a.Id, label));
        }

        RoomFilterOptions.Add(new LookupItem(null, "Alle Räume"));
        foreach (var r in _rooms.Values.OrderBy(r => r.Floor).ThenBy(r => r.Name))
        {
            RoomOptions.Add(new LookupItem(r.Id, RoomLabel(r)));
            RoomFilterOptions.Add(new LookupItem(r.Id, RoomLabel(r)));
        }
        foreach (var f in _functions.Values.OrderBy(f => f.SortOrder).ThenBy(f => f.Name))
            FunctionOptions.Add(new LookupItem(f.Id, f.Name));
        NewRoomId ??= RoomOptions.FirstOrDefault()?.Id;
        NewFunctionId ??= FunctionOptions.FirstOrDefault()?.Id;

        var cables = _data.GetCableSummaryByRoomFunction(value.Id);
        var rows = _data.GetRoomFunctions(value.Id)
            .Where(rf => _rooms.ContainsKey(rf.RoomId) && _functions.ContainsKey(rf.FunctionId))
            .Select(rf => CreateRow(rf, cables))
            .OrderBy(r => _rooms[r.Entity.RoomId].Floor)
            .ThenBy(r => _rooms[r.Entity.RoomId].Name)
            .ThenBy(r => r.FunctionOrder)
            .ThenBy(r => r.Entity.Circuit);

        foreach (var row in rows)
        {
            _autoSaver.Track(row.Entity);
            Rows.Add(row);
        }

        RefreshStatus();
    }

    private RoomFunctionRow CreateRow(RoomFunction rf, IReadOnlyDictionary<(int, int), string> cables)
    {
        var function = _functions[rf.FunctionId];
        return new RoomFunctionRow(rf, this,
            RoomLabel(_rooms[rf.RoomId]),
            function.Name,
            function.SortOrder,
            cables.GetValueOrDefault((rf.RoomId, rf.FunctionId), "– (nicht in Kabelliste)"));
    }

    private void ClearRows()
    {
        _autoSaver.UntrackAll<RoomFunction>();
        foreach (var row in Rows)
            row.Detach();
        Rows.Clear();
    }

    private static string RoomLabel(Room r) =>
        string.IsNullOrWhiteSpace(r.Floor) ? r.Name : $"{r.Floor} – {r.Name}";

    // ---------- Kanäle ----------

    public IReadOnlyList<LookupItem> BuildChannelOptions(int? actuatorId)
    {
        if (actuatorId is not int id
            || !_actuators.TryGetValue(id, out var actuator)
            || !_templates.TryGetValue(actuator.TemplateId, out var template))
            return new[] { LookupItem.None };

        return new[] { LookupItem.None }
            .Concat(Enumerable.Range(1, template.ChannelCount).Select(ch => new LookupItem(ch, $"K{ch}")))
            .ToList();
    }

    /// <summary>Erster Kanal des Aktors, dem noch keine andere Raum-Funktion zugewiesen ist.</summary>
    public int? FindFreeChannel(int actuatorId, RoomFunction except)
    {
        if (!_actuators.TryGetValue(actuatorId, out var actuator)
            || !_templates.TryGetValue(actuator.TemplateId, out var template))
            return null;

        var used = Rows.Select(r => r.Entity)
            .Where(rf => rf != except && rf.ActuatorId == actuatorId && rf.Channel != null)
            .Select(rf => rf.Channel!.Value)
            .ToHashSet();

        for (var ch = 1; ch <= template.ChannelCount; ch++)
        {
            if (!used.Contains(ch))
                return ch;
        }
        return null;
    }

    private void RefreshStatus()
    {
        var all = Rows.Select(r => r.Entity).ToList();
        foreach (var row in Rows)
        {
            var check = ActuatorLoadCheck.Check(row.Entity, _actuators, _templates, all);
            row.State = check.State;
            row.Status = check.Status;
        }

        var assigned = Rows.Count(r => r.Entity.ActuatorId != null);
        var overload = Rows.Count(r => r.State == LoadState.Overload);
        var warnings = Rows.Count(r => r.State == LoadState.Warning);
        Summary = $"{Rows.Count} Raum-Funktionen  ·  {assigned} zugewiesen  ·  {Rows.Count - assigned} offen" +
                  (overload > 0 ? $"  ·  ⛔ {overload} Überlast" : string.Empty) +
                  (warnings > 0 ? $"  ·  ⚠ {warnings} Hinweise" : string.Empty);
    }

    /// <summary>Filter neu anwenden. Eine noch offene Zellbearbeitung wird vorher übernommen.</summary>
    private void RefreshFilter()
    {
        if (RowsView is IEditableCollectionView editable)
        {
            if (editable.IsEditingItem)
                editable.CommitEdit();
            if (editable.IsAddingNew)
                editable.CommitNew();
        }
        RowsView.Refresh();
    }

    // ---------- Befehle ----------

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void SyncFromCables()
    {
        if (SelectedProject == null)
            return;

        var added = _data.SyncRoomFunctionsFromCables(SelectedProject.Id);
        Load(SelectedProject.Id);
        _dialogs.Info(added == 0
            ? "Alle Raum-Funktionen aus den Kabellisten sind bereits in der Tabelle."
            : $"{added} neue Raum-Funktion(en) aus den Kabellisten übernommen.");
    }

    private bool HasRow() => SelectedRow != null;

    /// <summary>Teilt die ausgewählte Raum-Funktion auf: legt einen weiteren Kreis an.</summary>
    [RelayCommand(CanExecute = nameof(HasRow))]
    private void AddCircuit()
    {
        if (SelectedProject == null || SelectedRow == null)
            return;

        var source = SelectedRow.Entity;
        InsertRow(_data.AddCircuit(SelectedProject.Id, source.RoomId, source.FunctionId), SelectedRow);
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddRoomFunction()
    {
        if (SelectedProject == null)
            return;

        if (NewRoomId is not int roomId || NewFunctionId is not int functionId)
        {
            _dialogs.Info("Bitte Raum und Funktion auswählen.\nRäume werden auf der Startseite angelegt.");
            return;
        }

        var after = Rows.LastOrDefault(r => r.Entity.RoomId == roomId && r.Entity.FunctionId == functionId)
                    ?? Rows.LastOrDefault(r => r.Entity.RoomId == roomId);
        InsertRow(_data.AddCircuit(SelectedProject.Id, roomId, functionId), after);
    }

    private void InsertRow(RoomFunction rf, RoomFunctionRow? after)
    {
        var row = CreateRow(rf, _data.GetCableSummaryByRoomFunction(rf.ProjectId));
        _autoSaver.Track(rf);

        // hinter dem letzten Kreis derselben Raum-Funktion einsortieren
        var last = Rows.LastOrDefault(r => r.Entity.RoomId == rf.RoomId && r.Entity.FunctionId == rf.FunctionId) ?? after;
        var index = last == null ? Rows.Count : Rows.IndexOf(last) + 1;
        Rows.Insert(index, row);
        SelectedRow = row;
        RefreshStatus();
    }

    [RelayCommand(CanExecute = nameof(HasRow))]
    private void DeleteRow()
    {
        if (SelectedRow is not { } row)
            return;

        var label = $"{row.RoomLabel} – {row.FunctionName} (Kreis {row.Entity.Circuit})";
        var hint = row.Cables.StartsWith("–")
            ? string.Empty
            : "\n\nHinweis: Diese Raum-Funktion kommt in den Kabellisten vor. Wenn es der einzige Kreis ist, " +
              "wird sie beim nächsten Öffnen automatisch wieder angelegt.";
        if (!_dialogs.Confirm($"\"{label}\" wirklich löschen?{hint}"))
            return;

        _data.Delete(row.Entity);
        _autoSaver.Untrack(row.Entity);
        row.Detach();
        Rows.Remove(row);
        RefreshStatus();
    }
}
