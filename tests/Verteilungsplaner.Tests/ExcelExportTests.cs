using ClosedXML.Excel;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.Tests;

public class ExcelExportTests
{
    [Fact]
    public void Export_WritesHeaderRowsAndCables()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, board, list) = PlannerDataServiceTests.CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Wohnzimmer", Floor = "EG" });
        var function = s.GetFunctions().First(f => f.Name == "Steckdosen");

        var cable = s.AddCable(list.Id);
        cable.Designation = "Steckdosen Fensterseite";
        cable.RoomId = room.Id;
        cable.FunctionId = function.Id;
        cable.Length = 15;
        cable.AdditionalText = "Extra";
        s.Update(cable);
        var second = s.AddCable(list.Id);
        second.Length = 5.5;
        s.Update(second);

        var file = Path.Combine(Path.GetTempPath(), $"vp-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            CableListExcelExporter.Export(file, project, board, list,
                s.GetCables(list.Id), s.GetRooms(project.Id), s.GetFunctions());

            using var wb = new XLWorkbook(file);
            var ws = wb.Worksheet(1);
            Assert.Equal("Kabelliste: Kabelliste EG", ws.Cell(1, 1).GetString());
            Assert.Equal("Kabel-Nr.", ws.Cell(6, 2).GetString());
            Assert.Equal("W1", ws.Cell(7, 2).GetString());
            Assert.Equal("Wohnzimmer", ws.Cell(7, 5).GetString());
            Assert.Equal("Steckdosen", ws.Cell(7, 6).GetString());
            Assert.Equal("Extra", ws.Cell(7, 13).GetString());
            Assert.Equal("W2", ws.Cell(8, 2).GetString());
            Assert.Equal(20.5, ws.Cell(10, 9).GetDouble());
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Export_EmptyList_DoesNotThrow()
    {
        var file = Path.Combine(Path.GetTempPath(), $"vp-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            CableListExcelExporter.Export(file,
                new Project { Name = "P" }, new DistributionBoard { Name = "UV" },
                new CableList { Name = "Liste: [1]" }, new List<Cable>(), new List<Room>(), new List<CableFunction>());
            Assert.True(File.Exists(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
