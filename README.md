# Verteilungsplaner

Windows-Software (WPF, .NET 8) zur Planung von Elektro-Verteilungen.

## Funktionen (Version 0.2)

- **Start**: Projekte anlegen (Name, Projektnummer, Kunde, Adresse, Beschreibung).
  Je Projekt werden die **Unterverteilungen (UV)** und **Räume** (mit Geschoss) gepflegt.
- **Kabellisten**: Eine Kabelliste gehört immer zu einem Projekt und einer UV.
  Je Kabel: Kabel-Nr., Bezeichnung, **Raum**, **Funktion**, Kabeltyp, Querschnitt, Länge,
  Absicherung, Klemme, Bemerkung und Zusatztext.
  - Kabel-Nr. wird automatisch vorgeschlagen (W1, W2, … – übernimmt auch eigene Präfixe wie `-W`).
  - Kabel duplizieren (Strg+D), neues Kabel (Strg+N), Reihenfolge per ▲/▼.
  - Räume können auch direkt in der Kabelliste angelegt werden.
  - Anzahl und Gesamtlänge werden angezeigt.
  - **Excel-Export** (.xlsx) mit Kopfdaten, Filter und Summenzeile.
- **Raum-Funktionen**: Tabelle aller Funktionen je Raum – wird automatisch aus den Kabellisten
  befüllt (jede Kombination Raum + Funktion). Eine Raum-Funktion kann in mehrere **Kreise**
  aufgeteilt werden (z. B. Wohnzimmer – Beleuchtung Kreis 1, 2, 3). Je Zeile:
  - Last in **A** und/oder **W** eintragen,
  - einem **Schalt- oder Dimmaktor** und einem **Kanal** zuweisen (freier Kanal wird vorgeschlagen),
  - automatische **Lastprüfung**: Kanal vorhanden? Summe aller Lasten am Kanal ≤ max. A / max. W?
    Überlast wird rot, Hinweise gelb markiert. UP-Aktoren in einem anderen Raum werden gemeldet.
  - Filter nach Raum und „nur ohne Aktor“.
- **Aktoren**: Aktoren des Projekts aus den Vorlagen anlegen (A1, A2, …). Aktoren für die
  Verteilung werden einer **UV**, UP-Aktoren einem **Raum** zugeordnet. Belegung je Aktor
  („3/8 belegt“) und Kanalübersicht mit zugewiesenen Raum-Funktionen und Last.
- **Einstellungen → Aktor-Vorlagen**: Grundkonfigurationen für alle Projekte mit Name, Typ
  (Schalt-/Dimmaktor), Einbauort (Verteilung/REG oder Raum/UP-Dose), Kanalanzahl,
  max. Strom je Kanal (A), max. Leistung je Kanal (W), TE, Hersteller und Artikel-Nr.
  Einige herstellerneutrale Vorlagen sind vorbelegt.
- **Einstellungen → Funktionen**: Funktionen (z. B. Beleuchtung, Steckdosen, Rollladen …) zentral für
  **alle** Kabellisten anlegen, umbenennen, sortieren und löschen. Beim ersten Start werden
  gängige Funktionen vorbelegt.

Alle Änderungen werden sofort automatisch gespeichert, in einer lokalen SQLite-Datenbank unter
`%LOCALAPPDATA%\Verteilungsplaner\verteilungsplaner.db`.

## Programm herunterladen

Bei jedem Push baut GitHub Actions eine fertige `Verteilungsplaner.exe`. Unter
**Actions → Build Verteilungsplaner → letzter Lauf → Artifacts → Verteilungsplaner-win-x64**
kann sie heruntergeladen werden. Es muss keine .NET-Installation vorhanden sein.

## Selbst bauen (Windows)

Voraussetzung: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet run --project src/Verteilungsplaner.App          # starten
dotnet test                                             # Tests
dotnet publish src/Verteilungsplaner.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Oder einfach `Verteilungsplaner.sln` in Visual Studio 2022 öffnen.

## Projektstruktur

| Ordner | Inhalt |
| --- | --- |
| `src/Verteilungsplaner.Core` | Datenmodell, Datenbank (EF Core + SQLite, Migrationen), Excel-Export |
| `src/Verteilungsplaner.App` | WPF-Oberfläche (MVVM mit CommunityToolkit.Mvvm) |
| `tests/Verteilungsplaner.Tests` | Automatische Tests (xUnit) |

### Datenbank-Änderungen

Das Schema wird über EF-Core-Migrationen verwaltet und beim Programmstart automatisch aktualisiert.
Neue Migration anlegen:

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/Verteilungsplaner.Core --output-dir Data/Migrations
```
