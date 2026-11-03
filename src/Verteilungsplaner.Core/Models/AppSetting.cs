namespace Verteilungsplaner.Core.Models;

/// <summary>Einfache Schlüssel/Wert-Einstellung (z. B. ob Standarddaten schon angelegt wurden).</summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }
}
