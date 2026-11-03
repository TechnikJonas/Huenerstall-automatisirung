namespace Verteilungsplaner.App.ViewModels;

/// <summary>Eintrag für Auswahllisten (Raum, Funktion). Id = null bedeutet "keine Zuordnung".</summary>
public record LookupItem(int? Id, string Name)
{
    public static LookupItem None { get; } = new(null, "—");
}
