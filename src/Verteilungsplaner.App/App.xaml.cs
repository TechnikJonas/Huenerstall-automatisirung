using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Verteilungsplaner.App.Services;
using Verteilungsplaner.App.ViewModels;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.App;

public partial class App : Application
{
    /// <summary>Speicherort der Datenbank: %LOCALAPPDATA%\Verteilungsplaner\verteilungsplaner.db</summary>
    public static string DatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Verteilungsplaner",
        "verteilungsplaner.db");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Zahlen/Datumsangaben in der Oberfläche im deutschen Format (Komma als Dezimaltrenner)
        var culture = CultureInfo.CurrentCulture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        DispatcherUnhandledException += OnUnhandledException;

        var dialogs = new DialogService();
        PlannerDataService data;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            data = new PlannerDataService(DatabasePath);
            data.Initialize();
        }
        catch (Exception ex)
        {
            dialogs.Error($"Die Datenbank konnte nicht geöffnet werden:\n{DatabasePath}\n\n{ex.Message}");
            Shutdown(1);
            return;
        }

        var main = new MainViewModel(
            new StartViewModel(data, dialogs),
            new CableListsViewModel(data, dialogs),
            new RoomFunctionsViewModel(data, dialogs),
            new ActuatorsViewModel(data, dialogs),
            new SettingsViewModel(data, dialogs));

        MainWindow = new MainWindow { DataContext = main };
        MainWindow.Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten:\n\n{e.Exception.Message}",
            "Verteilungsplaner", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
