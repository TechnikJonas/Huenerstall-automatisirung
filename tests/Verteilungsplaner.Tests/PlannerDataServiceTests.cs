using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.Tests;

public class PlannerDataServiceTests
{
    [Fact]
    public void Initialize_SeedsDefaultFunctionsOnce()
    {
        using var db = new TestDatabase();
        db.Service.Initialize(); // zweiter Aufruf darf nichts doppelt anlegen

        var functions = db.Service.GetFunctions();
        Assert.Equal(PlannerDataService.DefaultFunctions.Length, functions.Count);
        Assert.Equal("Beleuchtung", functions[0].Name);
    }

    [Fact]
    public void CableList_BelongsToProjectAndBoard()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var project = s.Add(new Project { Name = "EFH Muster" });
        var board = s.Add(new DistributionBoard { ProjectId = project.Id, Name = "UV-EG" });
        var list = s.Add(new CableList { ProjectId = project.Id, DistributionBoardId = board.Id, Name = "Kabelliste EG" });

        var lists = s.GetCableLists(project.Id);
        Assert.Single(lists);
        Assert.Equal(board.Id, lists[0].DistributionBoardId);
        Assert.Equal(list.Name, lists[0].Name);
    }

    [Fact]
    public void Cable_RoomAndFunction_AreStored()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, list) = CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Küche", Floor = "EG" });
        var function = s.GetFunctions().First(f => f.Name == "Herd");

        var cable = s.AddCable(list.Id);
        cable.RoomId = room.Id;
        cable.FunctionId = function.Id;
        cable.CableType = "NYM-J";
        cable.CrossSection = "5x2,5";
        cable.Length = 12.5;
        s.Update(cable);

        var loaded = Assert.Single(s.GetCables(list.Id));
        Assert.Equal(room.Id, loaded.RoomId);
        Assert.Equal(function.Id, loaded.FunctionId);
        Assert.Equal(12.5, loaded.Length);
        Assert.Equal("W1", loaded.Number);
    }

    [Fact]
    public void AddCable_SuggestsIncreasingNumbers()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (_, _, list) = CreateBasics(s);

        var c1 = s.AddCable(list.Id);
        var c2 = s.AddCable(list.Id);
        c2.Number = "-W10";
        s.Update(c2);
        var c3 = s.AddCable(list.Id);

        Assert.Equal("W1", c1.Number);
        Assert.Equal("-W11", c3.Number);
        Assert.True(c3.SortOrder > c2.SortOrder);
    }

    [Theory]
    [InlineData(new string[0], "W1")]
    [InlineData(new[] { "W1", "W2" }, "W3")]
    [InlineData(new[] { "W9", "W10", "" }, "W11")]
    [InlineData(new[] { "K5", "Licht" }, "K6")]
    public void SuggestNextCableNumber_Works(string[] existing, string expected)
    {
        Assert.Equal(expected, PlannerDataService.SuggestNextCableNumber(existing));
    }

    [Fact]
    public void DeletingFunction_KeepsCable_ButClearsAssignment()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (_, _, list) = CreateBasics(s);
        var function = s.Add(new CableFunction { Name = "Sonderfunktion" });
        var cable = s.AddCable(list.Id);
        cable.FunctionId = function.Id;
        s.Update(cable);

        s.Delete(function);

        var loaded = Assert.Single(s.GetCables(list.Id));
        Assert.Null(loaded.FunctionId);
    }

    [Fact]
    public void DeletingRoom_KeepsCable_ButClearsAssignment()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, list) = CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Bad" });
        var cable = s.AddCable(list.Id);
        cable.RoomId = room.Id;
        s.Update(cable);

        s.Delete(room);

        Assert.Null(Assert.Single(s.GetCables(list.Id)).RoomId);
    }

    [Fact]
    public void DeletingProject_RemovesEverythingBelongingToIt()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, list) = CreateBasics(s);
        s.Add(new Room { ProjectId = project.Id, Name = "Bad" });
        s.AddCable(list.Id);

        s.Delete(project);

        Assert.Empty(s.GetProjects());
        Assert.Empty(s.GetDistributionBoards(project.Id));
        Assert.Empty(s.GetRooms(project.Id));
        Assert.Empty(s.GetCableLists(project.Id));
        Assert.Empty(s.GetCables(list.Id));
        Assert.NotEmpty(s.GetFunctions()); // Funktionen sind global
    }

    [Fact]
    public void SaveCableOrder_RenumbersSortOrder()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (_, _, list) = CreateBasics(s);
        var a = s.AddCable(list.Id);
        var b = s.AddCable(list.Id);

        s.SaveCableOrder(new List<Cable> { b, a });

        var loaded = s.GetCables(list.Id);
        Assert.Equal(b.Id, loaded[0].Id);
        Assert.Equal(a.Id, loaded[1].Id);
    }

    internal static (Project, DistributionBoard, CableList) CreateBasics(PlannerDataService s)
    {
        var project = s.Add(new Project { Name = "Testprojekt", Number = "2026-001" });
        var board = s.Add(new DistributionBoard { ProjectId = project.Id, Name = "UV-EG", Location = "Flur" });
        var list = s.Add(new CableList { ProjectId = project.Id, DistributionBoardId = board.Id, Name = "Kabelliste EG" });
        return (project, board, list);
    }
}
