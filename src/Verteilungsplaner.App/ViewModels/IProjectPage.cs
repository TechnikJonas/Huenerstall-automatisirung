using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.App.ViewModels;

/// <summary>Seite, die sich auf ein ausgewähltes Projekt bezieht.</summary>
public interface IProjectPage
{
    Project? SelectedProject { get; }

    /// <summary>Lädt die Seite neu und wählt (falls vorhanden) das angegebene Projekt aus.</summary>
    void Load(int? projectId);
}
