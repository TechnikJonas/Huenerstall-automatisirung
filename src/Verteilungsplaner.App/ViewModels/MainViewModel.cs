using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(StartViewModel start, CableListsViewModel cableLists, SettingsViewModel settings)
    {
        Start = start;
        CableLists = cableLists;
        Settings = settings;

        Start.OpenCableListsRequested += (_, project) => ShowCableLists(project);

        _currentPage = Start;
        Start.Load();
    }

    public StartViewModel Start { get; }
    public CableListsViewModel CableLists { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private ObservableObject _currentPage;

    [RelayCommand]
    private void ShowStart()
    {
        Start.Load(CurrentProjectId);
        CurrentPage = Start;
    }

    [RelayCommand]
    private void ShowCableListsPage()
    {
        CableLists.Load(CurrentProjectId);
        CurrentPage = CableLists;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        Settings.Load();
        CurrentPage = Settings;
    }

    /// <summary>Das Projekt, das auf der aktuell angezeigten Seite ausgewählt ist.</summary>
    private int? CurrentProjectId =>
        CurrentPage == CableLists ? CableLists.SelectedProject?.Id ?? Start.SelectedProject?.Id : Start.SelectedProject?.Id;

    private void ShowCableLists(Project? project)
    {
        CableLists.Load(project?.Id);
        CurrentPage = CableLists;
    }
}
