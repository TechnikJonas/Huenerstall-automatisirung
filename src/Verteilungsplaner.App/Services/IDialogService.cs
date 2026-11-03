namespace Verteilungsplaner.App.Services;

public interface IDialogService
{
    bool Confirm(string message, string title = "Bitte bestätigen");

    void Info(string message, string title = "Hinweis");

    void Error(string message, string title = "Fehler");

    /// <summary>Zeigt einen "Speichern unter"-Dialog. Liefert den Pfad oder null bei Abbruch.</summary>
    string? SaveFile(string filter, string defaultFileName);
}
