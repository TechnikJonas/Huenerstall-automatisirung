using System.Windows;
using Microsoft.Win32;

namespace Verteilungsplaner.App.Services;

public class DialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    public bool Confirm(string message, string title = "Bitte bestätigen")
    {
        var result = Owner != null
            ? MessageBox.Show(Owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }

    public void Info(string message, string title = "Hinweis") =>
        Show(message, title, MessageBoxImage.Information);

    public void Error(string message, string title = "Fehler") =>
        Show(message, title, MessageBoxImage.Error);

    public string? SaveFile(string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true,
            OverwritePrompt = true,
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    private static void Show(string message, string title, MessageBoxImage image)
    {
        if (Owner != null)
            MessageBox.Show(Owner, message, title, MessageBoxButton.OK, image);
        else
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
    }
}
