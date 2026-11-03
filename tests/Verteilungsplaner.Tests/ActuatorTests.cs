using Microsoft.Data.Sqlite;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.Tests;

public class ActuatorTests
{
    [Fact]
    public void Initialize_SeedsActuatorTemplatesOnce_AndNotAgainAfterDeletion()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var templates = s.GetActuatorTemplates();
        Assert.Equal(PlannerDataService.CreateDefaultActuatorTemplates().Count, templates.Count);
        Assert.Contains(templates, t => t.Kind == ActuatorKind.Dimmer && t.Mounting == ActuatorMounting.Room);

        foreach (var t in templates)
            s.Delete(t);
        s.Initialize();

        Assert.Empty(s.GetActuatorTemplates());
    }

    [Fact]
    public void Template_InUse_CannotBeDeleted()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, board, _) = PlannerDataServiceTests.CreateBasics(s);
        var template = s.GetActuatorTemplates().First();
        s.Add(new Actuator { ProjectId = project.Id, TemplateId = template.Id, Name = "A1", DistributionBoardId = board.Id });

        Assert.Equal(1, s.CountActuatorsWithTemplate(template.Id));
        Assert.ThrowsAny<SqliteException>(() => s.Delete(template));
    }

    [Fact]
    public void SuggestNextActuatorName_CountsUp()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, _) = PlannerDataServiceTests.CreateBasics(s);
        var template = s.GetActuatorTemplates().First();

        Assert.Equal("A1", s.SuggestNextActuatorName(project.Id));
        s.Add(new Actuator { ProjectId = project.Id, TemplateId = template.Id, Name = "A1" });
        Assert.Equal("A2", s.SuggestNextActuatorName(project.Id));
    }

    [Fact]
    public void SyncRoomFunctionsFromCables_CreatesOneRowPerRoomAndFunction()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, list) = PlannerDataServiceTests.CreateBasics(s);
        var kitchen = s.Add(new Room { ProjectId = project.Id, Name = "Küche" });
        var light = s.GetFunctions().First(f => f.Name == "Beleuchtung");
        var sockets = s.GetFunctions().First(f => f.Name == "Steckdosen");

        AddCable(s, list.Id, kitchen.Id, light.Id);
        AddCable(s, list.Id, kitchen.Id, light.Id);   // gleiche Kombination
        AddCable(s, list.Id, kitchen.Id, sockets.Id);
        AddCable(s, list.Id, kitchen.Id, null);       // ohne Funktion → ignoriert

        Assert.Equal(2, s.SyncRoomFunctionsFromCables(project.Id));
        Assert.Equal(0, s.SyncRoomFunctionsFromCables(project.Id)); // nichts doppelt

        var summary = s.GetCableSummaryByRoomFunction(project.Id);
        Assert.Equal("W1, W2 (UV-EG)", summary[(kitchen.Id, light.Id)]);
    }

    [Fact]
    public void AddCircuit_UsesNextCircuitNumber()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, _) = PlannerDataServiceTests.CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Wohnzimmer" });
        var light = s.GetFunctions().First();

        var k1 = s.AddCircuit(project.Id, room.Id, light.Id);
        var k2 = s.AddCircuit(project.Id, room.Id, light.Id);

        Assert.Equal(1, k1.Circuit);
        Assert.Equal(2, k2.Circuit);
        Assert.Equal(2, s.GetRoomFunctions(project.Id).Count);
    }

    [Fact]
    public void DeleteActuator_FreesAssignedChannels()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, board, _) = PlannerDataServiceTests.CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Bad" });
        var actuator = s.Add(new Actuator { ProjectId = project.Id, TemplateId = s.GetActuatorTemplates().First().Id, Name = "A1", DistributionBoardId = board.Id });
        var rf = s.AddCircuit(project.Id, room.Id, s.GetFunctions().First().Id);
        rf.ActuatorId = actuator.Id;
        rf.Channel = 2;
        s.Update(rf);

        s.DeleteActuator(actuator);

        var loaded = Assert.Single(s.GetRoomFunctions(project.Id));
        Assert.Null(loaded.ActuatorId);
        Assert.Null(loaded.Channel);
    }

    [Fact]
    public void DeletingRoom_RemovesItsRoomFunctions()
    {
        using var db = new TestDatabase();
        var s = db.Service;
        var (project, _, _) = PlannerDataServiceTests.CreateBasics(s);
        var room = s.Add(new Room { ProjectId = project.Id, Name = "Bad" });
        s.AddCircuit(project.Id, room.Id, s.GetFunctions().First().Id);

        s.Delete(room);

        Assert.Empty(s.GetRoomFunctions(project.Id));
    }

    private static void AddCable(PlannerDataService s, int listId, int? roomId, int? functionId)
    {
        var c = s.AddCable(listId);
        c.RoomId = roomId;
        c.FunctionId = functionId;
        s.Update(c);
    }
}

public class ActuatorLoadCheckTests
{
    private static readonly ActuatorTemplate Switch8 = new()
    {
        Id = 1, Name = "SA 8x16A", Kind = ActuatorKind.Switch, ChannelCount = 8, MaxCurrentPerChannel = 16,
    };

    private static readonly ActuatorTemplate Dimmer = new()
    {
        Id = 2, Name = "DA 4x300W", Kind = ActuatorKind.Dimmer, ChannelCount = 4, MaxCurrentPerChannel = 1.3, MaxPowerPerChannel = 300,
    };

    private static RoomFunctionCheck Check(RoomFunction rf, Actuator a, params RoomFunction[] all) =>
        ActuatorLoadCheck.Check(rf,
            new Dictionary<int, Actuator> { [a.Id] = a },
            new Dictionary<int, ActuatorTemplate> { [Switch8.Id] = Switch8, [Dimmer.Id] = Dimmer },
            all.Length == 0 ? new[] { rf } : all);

    [Fact]
    public void Unassigned_IsReported()
    {
        var a = new Actuator { Id = 10, TemplateId = 1 };
        Assert.Equal(LoadState.Unassigned, Check(new RoomFunction(), a).State);
    }

    [Fact]
    public void WithinLimits_IsOk()
    {
        var a = new Actuator { Id = 10, TemplateId = 1 };
        var rf = new RoomFunction { Id = 1, ActuatorId = 10, Channel = 1, LoadCurrent = 10 };
        var result = Check(rf, a);
        Assert.Equal(LoadState.Ok, result.State);
        Assert.Contains("10 / 16 A", result.Status);
    }

    [Fact]
    public void SharedChannel_SumsLoad_AndDetectsOverload()
    {
        var a = new Actuator { Id = 10, TemplateId = 1 };
        var rf1 = new RoomFunction { Id = 1, ActuatorId = 10, Channel = 3, LoadCurrent = 10 };
        var rf2 = new RoomFunction { Id = 2, ActuatorId = 10, Channel = 3, LoadCurrent = 8.5 };

        var result = Check(rf1, a, rf1, rf2);

        Assert.Equal(LoadState.Overload, result.State);
        Assert.Contains("18,5 A > 16 A", result.Status);
    }

    [Fact]
    public void Dimmer_ChecksPower()
    {
        var a = new Actuator { Id = 10, TemplateId = 2 };
        var rf = new RoomFunction { Id = 1, ActuatorId = 10, Channel = 1, LoadPower = 350 };
        var result = Check(rf, a);
        Assert.Equal(LoadState.Overload, result.State);
        Assert.Contains("350 W > 300 W", result.Status);
    }

    [Fact]
    public void ChannelOutOfRange_IsWarning()
    {
        var a = new Actuator { Id = 10, TemplateId = 2 };
        var rf = new RoomFunction { Id = 1, ActuatorId = 10, Channel = 5, LoadPower = 50 };
        Assert.Equal(LoadState.Warning, Check(rf, a).State);
    }

    [Fact]
    public void MissingChannelOrLoad_IsWarning()
    {
        var a = new Actuator { Id = 10, TemplateId = 1 };
        Assert.Equal(LoadState.Warning, Check(new RoomFunction { Id = 1, ActuatorId = 10 }, a).State);
        Assert.Equal(LoadState.Warning, Check(new RoomFunction { Id = 1, ActuatorId = 10, Channel = 1 }, a).State);
    }

    [Fact]
    public void ChannelUsage_ListsAllChannels()
    {
        var a = new Actuator { Id = 10, TemplateId = 2 };
        var rf = new RoomFunction { Id = 1, ActuatorId = 10, Channel = 2, LoadPower = 100 };

        var usage = ActuatorLoadCheck.GetChannelUsage(a, Dimmer, new[] { rf });

        Assert.Equal(4, usage.Count);
        Assert.Equal(LoadState.Unassigned, usage[0].State);
        Assert.Equal(LoadState.Ok, usage[1].State);
        Assert.Equal(100, usage[1].TotalPower);
    }

    [Fact]
    public void UpActuatorInOtherRoom_IsWarning()
    {
        var up = new ActuatorTemplate { Id = 3, ChannelCount = 1, MaxCurrentPerChannel = 10, Mounting = ActuatorMounting.Room };
        var a = new Actuator { Id = 10, TemplateId = 3, RoomId = 99 };
        var rf = new RoomFunction { Id = 1, RoomId = 5, ActuatorId = 10, Channel = 1, LoadCurrent = 2 };

        var result = ActuatorLoadCheck.Check(rf,
            new Dictionary<int, Actuator> { [10] = a },
            new Dictionary<int, ActuatorTemplate> { [3] = up },
            new[] { rf });

        Assert.Equal(LoadState.Warning, result.State);
    }
}
