using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(
        StartViewModel start,
        CableListsViewModel cableLists,
        RoomFunctionsViewModel roomFunctions,
        ActuatorsViewModel actuators,
        SettingsViewModel settings)
    {
        Start = start;
        CableLists = cableLists;
        RoomFunctions = roomFunctions;
        Actuators = actuators;
        Settings = settings;

        Start.OpenCableListsRequested += (_, project) => ShowPage(CableLists, project.Id);

        _currentPage = Start;
        Start.Load();
    }

    public StartViewModel Start { get; }
    public CableListsViewModel CableLists { get; }
    public RoomFunctionsViewModel RoomFunctions { get; }
    public ActuatorsViewModel Actuators { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private ObservableObject _currentPage;

    /// <summary>Das Projekt, das auf der aktuell angezeigten Seite ausgewählt ist.</summary>
    private int? CurrentProjectId =>
        (CurrentPage as IProjectPage)?.SelectedProject?.Id ?? Start.SelectedProject?.Id;

    [RelayCommand]
    private void ShowStart() => ShowPage(Start, CurrentProjectId);

    [RelayCommand]
    private void ShowCableListsPage() => ShowPage(CableLists, CurrentProjectId);

    [RelayCommand]
    private void ShowRoomFunctions() => ShowPage(RoomFunctions, CurrentProjectId);

    [RelayCommand]
    private void ShowActuators() => ShowPage(Actuators, CurrentProjectId);

    [RelayCommand]
    private void ShowSettings()
    {
        Settings.Load();
        CurrentPage = Settings;
    }

    private void ShowPage<T>(T page, int? projectId) where T : ObservableObject, IProjectPage
    {
        page.Load(projectId);
        CurrentPage = page;
    }
}
