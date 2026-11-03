using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Startseite: Projekte, deren Unterverteilungen (UV) und Räume anlegen.</summary>
public partial class StartViewModel : ObservableObject
{
    private readonly PlannerDataService _data;
    private readonly IDialogService _dialogs;
    private readonly AutoSaver _autoSaver;

    public StartViewModel(PlannerDataService data, IDialogService dialogs)
    {
        _data = data;
        _dialogs = dialogs;
        _autoSaver = new AutoSaver(data, dialogs);
    }

    public event EventHandler<Project>? OpenCableListsRequested;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<DistributionBoard> Boards { get; } = new();
    public ObservableCollection<Room> Rooms { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProject))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand), nameof(AddBoardCommand), nameof(AddRoomCommand), nameof(OpenCableListsCommand))]
    private Project? _selectedProject;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteBoardCommand))]
    private DistributionBoard? _selectedBoard;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRoomCommand))]
    private Room? _selectedRoom;

    [ObservableProperty]
    private string _newBoardName = string.Empty;

    [ObservableProperty]
    private string _newRoomName = string.Empty;

    [ObservableProperty]
    private string _newRoomFloor = string.Empty;

    public bool HasProject => SelectedProject != null;

    /// <summary>Lädt die Projekte neu. Optional wird ein bestimmtes Projekt ausgewählt.</summary>
    public void Load(int? projectId = null)
    {
        var selectedId = projectId ?? SelectedProject?.Id;

        _autoSaver.UntrackAll<Project>();
        Projects.Clear();
        foreach (var p in _data.GetProjects())
        {
            _autoSaver.Track(p);
            Projects.Add(p);
        }

        SelectedProject = Projects.FirstOrDefault(p => p.Id == selectedId) ?? Projects.FirstOrDefault();
    }

    partial void OnSelectedProjectChanged(Project? value) => LoadProjectDetails();

    private void LoadProjectDetails()
    {
        _autoSaver.UntrackAll<DistributionBoard>();
        _autoSaver.UntrackAll<Room>();
        Boards.Clear();
        Rooms.Clear();

        if (SelectedProject == null)
            return;

        foreach (var b in _data.GetDistributionBoards(SelectedProject.Id))
        {
            _autoSaver.Track(b);
            Boards.Add(b);
        }

        foreach (var r in _data.GetRooms(SelectedProject.Id))
        {
            _autoSaver.Track(r);
            Rooms.Add(r);
        }
    }

    // ---------- Projekte ----------

    [RelayCommand]
    private void AddProject()
    {
        var name = "Neues Projekt";
        var n = 1;
        while (Projects.Any(p => p.Name == name))
            name = $"Neues Projekt {++n}";

        var project = _data.Add(new Project { Name = name });
        _autoSaver.Track(project);
        Projects.Add(project);
        SelectedProject = project;
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void DeleteProject()
    {
        if (SelectedProject == null)
            return;

        if (!_dialogs.Confirm(
                $"Projekt \"{SelectedProject.Name}\" wirklich löschen?\n\n" +
                "Alle UVs, Räume und Kabellisten dieses Projekts werden ebenfalls gelöscht."))
            return;

        var project = SelectedProject;
        _data.Delete(project);
        _autoSaver.Untrack(project);
        Projects.Remove(project);
        SelectedProject = Projects.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void OpenCableLists()
    {
        if (SelectedProject != null)
            OpenCableListsRequested?.Invoke(this, SelectedProject);
    }

    // ---------- Unterverteilungen ----------

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddBoard()
    {
        if (SelectedProject == null)
            return;

        var name = NewBoardName.Trim();
        if (name.Length == 0)
            name = $"UV{Boards.Count + 1}";

        var board = _data.Add(new DistributionBoard { ProjectId = SelectedProject.Id, Name = name });
        _autoSaver.Track(board);
        Boards.Add(board);
        SelectedBoard = board;
        NewBoardName = string.Empty;
    }

    private bool CanDeleteBoard() => SelectedBoard != null;

    [RelayCommand(CanExecute = nameof(CanDeleteBoard))]
    private void DeleteBoard()
    {
        if (SelectedBoard == null)
            return;

        var board = SelectedBoard;
        var listCount = _data.CountCableListsOfBoard(board.Id);
        var message = $"UV \"{board.Name}\" wirklich löschen?";
        if (listCount > 0)
            message += $"\n\nAchtung: {listCount} Kabelliste(n) dieser UV werden ebenfalls gelöscht!";

        if (!_dialogs.Confirm(message))
            return;

        _data.Delete(board);
        _autoSaver.Untrack(board);
        Boards.Remove(board);
    }

    // ---------- Räume ----------

    [RelayCommand(CanExecute = nameof(HasProject))]
    private void AddRoom()
    {
        if (SelectedProject == null)
            return;

        var name = NewRoomName.Trim();
        if (name.Length == 0)
        {
            _dialogs.Info("Bitte einen Raumnamen eingeben.");
            return;
        }

        var floor = string.IsNullOrWhiteSpace(NewRoomFloor) ? null : NewRoomFloor.Trim();
        var room = _data.Add(new Room { ProjectId = SelectedProject.Id, Name = name, Floor = floor });
        _autoSaver.Track(room);
        Rooms.Add(room);
        SelectedRoom = room;
        NewRoomName = string.Empty;
    }

    private bool CanDeleteRoom() => SelectedRoom != null;

    [RelayCommand(CanExecute = nameof(CanDeleteRoom))]
    private void DeleteRoom()
    {
        if (SelectedRoom == null)
            return;

        var room = SelectedRoom;
        var used = _data.CountCablesInRoom(room.Id);
        var message = $"Raum \"{room.Name}\" wirklich löschen?";
        if (used > 0)
            message += $"\n\n{used} Kabel sind diesem Raum zugeordnet. Die Kabel bleiben erhalten, die Raumzuordnung wird entfernt.";

        if (!_dialogs.Confirm(message))
            return;

        _data.Delete(room);
        _autoSaver.Untrack(room);
        Rooms.Remove(room);
    }
}
