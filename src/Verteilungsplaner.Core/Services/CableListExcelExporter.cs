using ClosedXML.Excel;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.Core.Services;

/// <summary>Exportiert eine Kabelliste als Excel-Datei (.xlsx).</summary>
public static class CableListExcelExporter
{
    public static readonly string[] Headers =
    {
        "Pos.", "Kabel-Nr.", "Bezeichnung", "Geschoss", "Raum", "Funktion", "Kabeltyp",
        "Querschnitt", "Länge (m)", "Absicherung", "Klemme", "Bemerkung", "Zusatztext",
    };

    public static void Export(
        string filePath,
        Project project,
        DistributionBoard board,
        CableList cableList,
        IReadOnlyList<Cable> cables,
        IReadOnlyCollection<Room> rooms,
        IReadOnlyCollection<CableFunction> functions)
    {
        var roomById = rooms.ToDictionary(r => r.Id);
        var functionById = functions.ToDictionary(f => f.Id);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SafeSheetName(cableList.Name));

        // Kopfbereich
        ws.Cell(1, 1).Value = $"Kabelliste: {cableList.Name}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = "Projekt:";
        ws.Cell(2, 2).Value = string.IsNullOrWhiteSpace(project.Number) ? project.Name : $"{project.Number} – {project.Name}";
        ws.Cell(3, 1).Value = "UV:";
        ws.Cell(3, 2).Value = string.IsNullOrWhiteSpace(board.Location) ? board.Name : $"{board.Name} ({board.Location})";
        ws.Cell(4, 1).Value = "Stand:";
        ws.Cell(4, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        ws.Range(2, 1, 4, 1).Style.Font.Bold = true;

        // Tabelle
        const int headerRow = 6;
        for (var col = 0; col < Headers.Length; col++)
            ws.Cell(headerRow, col + 1).Value = Headers[col];

        var row = headerRow;
        var pos = 0;
        foreach (var cable in cables)
        {
            row++;
            pos++;
            roomById.TryGetValue(cable.RoomId ?? -1, out var room);
            functionById.TryGetValue(cable.FunctionId ?? -1, out var function);

            ws.Cell(row, 1).Value = pos;
            ws.Cell(row, 2).Value = cable.Number;
            ws.Cell(row, 3).Value = cable.Designation;
            ws.Cell(row, 4).Value = room?.Floor;
            ws.Cell(row, 5).Value = room?.Name;
            ws.Cell(row, 6).Value = function?.Name;
            ws.Cell(row, 7).Value = cable.CableType;
            ws.Cell(row, 8).Value = cable.CrossSection;
            if (cable.Length.HasValue)
                ws.Cell(row, 9).Value = cable.Length.Value;
            ws.Cell(row, 10).Value = cable.Fuse;
            ws.Cell(row, 11).Value = cable.Terminal;
            ws.Cell(row, 12).Value = cable.Remark;
            ws.Cell(row, 13).Value = cable.AdditionalText;
        }

        var table = ws.Range(headerRow, 1, Math.Max(row, headerRow + 1), Headers.Length)
            .CreateTable("Kabelliste");
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;

        ws.Column(9).Style.NumberFormat.Format = "0.0";

        // Summenzeile
        var sumRow = Math.Max(row, headerRow + 1) + 2;
        ws.Cell(sumRow, 8).Value = "Summe Länge:";
        ws.Cell(sumRow, 8).Style.Font.Bold = true;
        ws.Cell(sumRow, 9).Value = cables.Sum(c => c.Length ?? 0);
        ws.Cell(sumRow, 9).Style.Font.Bold = true;
        ws.Cell(sumRow + 1, 8).Value = "Anzahl Kabel:";
        ws.Cell(sumRow + 1, 8).Style.Font.Bold = true;
        ws.Cell(sumRow + 1, 9).Value = cables.Count;
        ws.Cell(sumRow + 1, 9).Style.NumberFormat.Format = "0";

        ws.SheetView.FreezeRows(headerRow);
        ws.Columns().AdjustToContents(headerRow, row);
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 10);

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

        workbook.SaveAs(filePath);
    }

    private static string SafeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = "Kabelliste";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
