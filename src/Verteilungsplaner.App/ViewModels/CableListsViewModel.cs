using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Kabellisten eines Projekts anlegen und bearbeiten.</summary>
public partial class CableListsViewModel : ObservableObject, IProjectPage
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly AutoSaver _autoSaver;
    private int? _preferredListId;

    public CableListsViewModel(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
        _autoSaver = new AutoSaver(data, dialogs);
        _autoSaver.Saved += (_, e) =>
        {
            if (e is Cable)
                UpdateSummary();
        };
    }

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<DistributionBoard> Boards { get; } = new();
    public ObservableCollection<CableList> CableLists { get; } = new();
    public ObservableCollection<Cable> Cables { get; } = new();

    /// <summary>Auswahl für die Raum-Spalte (inkl. "—" für keine Zuordnung).</summary>
    public ObservableCollection<LookupItem> RoomOptions { get; } = new();

    /// <summary>Auswahl für die Funktions-Spalte (aus den Einstellungen).</summary>
    public ObservableCollection<LookupItem> FunctionOptions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProject))]
    [NotifyCanExecuteChangedFor(nameof(CreateCableListCommand), nameof(AddRoomCommand))]
    private Project? _selectedProject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCableList), nameof(SelectedCableListTitle))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCableListCommand), nameof(AddCableCommand), nameof(ExportExcelCommand))]
    private CableList? _selectedCableList;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCableCommand), nameof(DuplicateCableCommand), nameof(MoveUpCommand), nameof(MoveDownCommand))]
    private Cable? _selectedCable;

    [ObservableProperty]
    private string _newListName = string.Empty;

    [ObservableProperty]
    private DistributionBoard? _newListBoard;

    [ObservableProperty]
    private string _quickRoomName = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    public bool HasProject => SelectedProject != null;

    public bool HasCableList => SelectedCableList != null;

    public string SelectedCableListTitle
    {
        get
        {
            if (SelectedCableList == null)
                return "Keine Kabelliste ausgewählt";
            var board = Boards.FirstOrDefault(b => b.Id == SelectedCableList.DistributionBoardId);
            return board == null ? SelectedCableList.Name : $"{SelectedCableList.Name}  ·  {board.Name}";
        }
    }

    /// <summary>Lädt die Seite neu. Optional wird ein bestimmtes Projekt ausgewählt.</summary>
    public void Load(int? projectId)
    {
        var keepProjectId = projectId ?? SelectedProject?.Id;
        _preferredListId = SelectedCableList?.Id;

        // Auswahl zurücksetzen, damit alles (Räume, Funktionen, UVs) frisch geladen wird
        SelectedProject = null;
        Projects.Clear();
        foreach (var p in _data.GetProjects())
            Projects.Add(p);

        SelectedProject = Projects.FirstOrDefault(p => p.Id == keepProjectId) ?? Projects.FirstOrDefault();
    }

    partial void OnSelectedProjectChanged(Project? value)
    {
        // Reihenfolge wichtig: erst Kabel und Listen leeren, dann die Auswahllisten,
        // sonst setzen die Auswahlfelder in der Tabelle Werte auf "leer".
        SelectedCableList = null;
        ClearCables();
        _autoSaver.UntrackAll<CableList>();
        CableLists.Clear();
        Boards.Clear();
        RoomOptions.Clear();
        FunctionOptions.Clear();

        if (value == null)
            return;

        foreach (var b in _data.GetDistributionBoards(value.Id))
            Boards.Add(b);
        NewListBoard = Boards.FirstOrDefault();

        RoomOptions.Add(LookupItem.None);
        foreach (var r in _data.GetRooms(value.Id))
            RoomOptions.Add(new LookupItem(r.Id, RoomLabel(r)));

        FunctionOptions.Add(LookupItem.None);
        foreach (var f in _data.GetFunctions())
            FunctionOptions.Add(new LookupItem(f.Id, f.Name));

        foreach (var l in _data.GetCableLists(value.Id))
        {
            _autoSaver.Track(l);
            CableLists.Add(l);
        }

        SelectedCableList = CableLists.FirstOrDefault(l => l.Id == _preferredListId) ?? CableLists.FirstOrDefault();
        _preferredListId = null;
    }

    partial void OnSelectedCableListChanged(CableList? value) => LoadCables();

    private void LoadCables()
    {
        ClearCables();
        if (SelectedCableList != null)
        {
            foreach (var c in _data.GetCables(SelectedCableList.Id))
            {
                _autoSaver.Track(c);
                Cables.Add(c);
            }
        }
        UpdateSummary();
    }

    private void ClearCables()
    {
        // Erst vom Autospeichern abmelden, damit beim Leeren der Tabelle nichts überschrieben wird.
        _autoSaver.UntrackAll<Cable>();
        Cables.Clear();
    }

    private void UpdateSummary()
    {
        var total = Cables.Sum(c => c.Length ?? 0);
        Summary = $"{Cables.Count} Kabel  ·  Gesamtlänge {total:0.0} m";
    }

    private static string RoomLabel(Room r) =>
        string.IsNullOrWhiteSpace(r.Floor) ? r.Name : $"{r.Floor} – {r.Name}";

    // ---------- Kabellisten ----------

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void CreateCableList()
    {
        if (SelectedProject == null)
            return;

        if (Boards.Count == 0)
        {
            _dialogs.Info("Für dieses Projekt ist noch keine UV angelegt.\nBitte zuerst auf der Startseite eine UV anlegen.");
            return;
        }

        if (NewListBoard == null)
        {
            _dialogs.Info("Bitte eine UV für die neue Kabelliste auswählen.");
            return;
        }

        var name = NewListName.Trim();
        if (name.Length == 0)
            name = $"Kabelliste {NewListBoard.Name}";

        var list = _data.Add(new CableList
        {
            ProjectId = SelectedProject.Id,
            DistributionBoardId = NewListBoard.Id,
            Name = name,
        });
        _autoSaver.Track(list);
        CableLists.Add(list);
        SelectedCableList = list;
        NewListName = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(HasCableList))]
    private void DeleteCableList()
    {
        if (SelectedCableList == null)
            return;

        var list = SelectedCableList;
        if (!_dialogs.Confirm($"Kabelliste \"{list.Name}\" mit {Cables.Count} Kabel(n) wirklich löschen?"))
            return;

        _data.Delete(list);
        _autoSaver.Untrack(list);
        CableLists.Remove(list);
        SelectedCableList = CableLists.FirstOrDefault();
    }

    // ---------- Kabel ----------

    [RelayCommand(CanExecute = nameof(HasCableList))]
    private void AddCable() => InsertCable(null);

    private bool HasCable() => SelectedCable != null;

    [RelayCommand(CanExecute = nameof(HasCable))]
    private void DuplicateCable() => InsertCable(SelectedCable);

    private void InsertCable(Cable? template)
    {
        if (SelectedCableList == null)
            return;

        var cable = _data.AddCable(SelectedCableList.Id, template);
        _autoSaver.Track(cable);
        Cables.Add(cable);
        SelectedCable = cable;
        UpdateSummary();
    }

    [RelayCommand(CanExecute = nameof(HasCable))]
    private void DeleteCable()
    {
        if (SelectedCable == null)
            return;

        var cable = SelectedCable;
        var label = string.IsNullOrWhiteSpace(cable.Designation) ? cable.Number : $"{cable.Number} ({cable.Designation})";
        if (!_dialogs.Confirm($"Kabel {label} wirklich löschen?"))
            return;

        var index = Cables.IndexOf(cable);
        _data.Delete(cable);
        _autoSaver.Untrack(cable);
        Cables.Remove(cable);
        SelectedCable = Cables.Count == 0 ? null : Cables[Math.Min(index, Cables.Count - 1)];
        UpdateSummary();
    }

    [RelayCommand(CanExecute = nameof(HasCable))]
    private void MoveUp() => MoveCable(-1);

    [RelayCommand(CanExecute = nameof(HasCable))]
    private void MoveDown() => MoveCable(+1);

    private void MoveCable(int delta)
    {
        if (SelectedCable == null)
            return;

        var cable = SelectedCable;
        var index = Cables.IndexOf(cable);
        var target = index + delta;
        if (target < 0 || target >= Cables.Count)
            return;

        Cables.Move(index, target);
        _autoSaver.UntrackAll<Cable>();
        _data.SaveCableOrder(Cables);
        foreach (var c in Cables)
            _autoSaver.Track(c);
        SelectedCable = cable;
    }

    // ---------- Räume (Schnellanlage) ----------

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddRoom()
    {
        if (SelectedProject == null)
            return;

        var name = QuickRoomName.Trim();
        if (name.Length == 0)
        {
            _dialogs.Info("Bitte einen Raumnamen eingeben.");
            return;
        }

        var room = _data.Add(new Room { ProjectId = SelectedProject.Id, Name = name });
        RoomOptions.Add(new LookupItem(room.Id, RoomLabel(room)));
        QuickRoomName = string.Empty;

        if (SelectedCable != null && SelectedCable.RoomId == null)
            SelectedCable.RoomId = room.Id;
    }

    // ---------- Export ----------

    [RelayCommand(CanExecute = nameof(HasCableList))]
    private void ExportExcel()
    {
        if (SelectedProject == null || SelectedCableList == null)
            return;

        var list = SelectedCableList;
        var board = Boards.FirstOrDefault(b => b.Id == list.DistributionBoardId);
        if (board == null)
            return;

        var defaultName = MakeFileName($"{SelectedProject.Name} - {board.Name} - {list.Name}.xlsx");
        var path = _dialogs.SaveFile("Excel-Arbeitsmappe (*.xlsx)|*.xlsx", defaultName);
        if (path == null)
            return;

        try
        {
            CableListExcelExporter.Export(path, SelectedProject, board, list,
                _data.GetCables(list.Id), _data.GetRooms(SelectedProject.Id), _data.GetFunctions());
            _dialogs.Info($"Die Kabelliste wurde exportiert:\n{path}", "Export erfolgreich");
        }
        catch (IOException ex)
        {
            _dialogs.Error($"Die Datei konnte nicht geschrieben werden. Ist sie evtl. noch in Excel geöffnet?\n\n{ex.Message}");
        }
    }

    private static string MakeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
