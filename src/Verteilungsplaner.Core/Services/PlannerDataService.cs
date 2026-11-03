using Microsoft.EntityFrameworkCore;
using Verteilungsplaner.Core.Data;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.Core.Services;

/// <summary>
/// Zentraler Datenzugriff. Jede Operation verwendet einen eigenen, kurzlebigen DbContext,
/// die Objekte in der Oberfläche sind daher nicht an einen Kontext gebunden.
/// </summary>
public class PlannerDataService
{
    private readonly DbContextOptions<PlannerDbContext> _options;

    public PlannerDataService(string databasePath)
        : this(new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options)
    {
    }

    public PlannerDataService(DbContextOptions<PlannerDbContext> options)
    {
        _options = options;
    }

    public static readonly string[] DefaultFunctions =
    {
        "Beleuchtung",
        "Steckdosen",
        "Rollladen / Jalousie",
        "Herd",
        "Backofen",
        "Geschirrspüler",
        "Waschmaschine",
        "Trockner",
        "Heizung",
        "Lüftung",
        "Netzwerk / Daten",
        "Bus (KNX)",
        "Außenbeleuchtung",
    };

    /// <summary>Vorbelegte Aktor-Vorlagen (herstellerneutral), werden beim ersten Start angelegt.</summary>
    public static IReadOnlyList<ActuatorTemplate> CreateDefaultActuatorTemplates() => new[]
    {
        new ActuatorTemplate { Name = "Schaltaktor 4-fach 16 A", Kind = ActuatorKind.Switch, Mounting = ActuatorMounting.DistributionBoard, ChannelCount = 4, MaxCurrentPerChannel = 16, WidthTe = 4 },
        new ActuatorTemplate { Name = "Schaltaktor 8-fach 16 A", Kind = ActuatorKind.Switch, Mounting = ActuatorMounting.DistributionBoard, ChannelCount = 8, MaxCurrentPerChannel = 16, WidthTe = 8 },
        new ActuatorTemplate { Name = "Schaltaktor 12-fach 16 A", Kind = ActuatorKind.Switch, Mounting = ActuatorMounting.DistributionBoard, ChannelCount = 12, MaxCurrentPerChannel = 16, WidthTe = 12 },
        new ActuatorTemplate { Name = "Dimmaktor 4-fach 300 W", Kind = ActuatorKind.Dimmer, Mounting = ActuatorMounting.DistributionBoard, ChannelCount = 4, MaxCurrentPerChannel = 1.3, MaxPowerPerChannel = 300, WidthTe = 4 },
        new ActuatorTemplate { Name = "UP-Schaltaktor 1-fach 10 A", Kind = ActuatorKind.Switch, Mounting = ActuatorMounting.Room, ChannelCount = 1, MaxCurrentPerChannel = 10 },
        new ActuatorTemplate { Name = "UP-Schaltaktor 2-fach 10 A", Kind = ActuatorKind.Switch, Mounting = ActuatorMounting.Room, ChannelCount = 2, MaxCurrentPerChannel = 10 },
        new ActuatorTemplate { Name = "UP-Dimmaktor 1-fach 200 W", Kind = ActuatorKind.Dimmer, Mounting = ActuatorMounting.Room, ChannelCount = 1, MaxCurrentPerChannel = 0.9, MaxPowerPerChannel = 200 },
    };

    private const string SeededFunctionsKey = "Seeded.Functions";
    private const string SeededActuatorTemplatesKey = "Seeded.ActuatorTemplates";

    private PlannerDbContext CreateContext() => new(_options);

    /// <summary>Legt die Datenbank an bzw. aktualisiert sie und befüllt Standard-Funktionen.</summary>
    public void Initialize()
    {
        using var ctx = CreateContext();
        ctx.Database.Migrate();

        // Standarddaten nur einmal anlegen – wer sie löscht, bekommt sie nicht ungefragt wieder.
        if (!IsSeeded(ctx, SeededFunctionsKey))
        {
            if (!ctx.CableFunctions.Any())
            {
                var order = 0;
                foreach (var name in DefaultFunctions)
                    ctx.CableFunctions.Add(new CableFunction { Name = name, SortOrder = ++order });
            }
            ctx.AppSettings.Add(new AppSetting { Key = SeededFunctionsKey, Value = "1" });
            ctx.SaveChanges();
        }

        if (!IsSeeded(ctx, SeededActuatorTemplatesKey))
        {
            if (!ctx.ActuatorTemplates.Any())
                ctx.ActuatorTemplates.AddRange(CreateDefaultActuatorTemplates());
            ctx.AppSettings.Add(new AppSetting { Key = SeededActuatorTemplatesKey, Value = "1" });
            ctx.SaveChanges();
        }
    }

    private static bool IsSeeded(PlannerDbContext ctx, string key) => ctx.AppSettings.Any(s => s.Key == key);

    // ---------- Allgemein ----------

    public T Add<T>(T entity) where T : ObservableEntity
    {
        using var ctx = CreateContext();
        ctx.Set<T>().Add(entity);
        ctx.SaveChanges();
        return entity;
    }

    public void Update<T>(T entity) where T : ObservableEntity
    {
        using var ctx = CreateContext();
        ctx.Set<T>().Update(entity);
        ctx.SaveChanges();
    }

    public void Delete<T>(T entity) where T : ObservableEntity
    {
        using var ctx = CreateContext();
        ctx.Set<T>().Where(e => e.Id == entity.Id).ExecuteDelete();
    }

    // ---------- Abfragen ----------

    public List<Project> GetProjects()
    {
        using var ctx = CreateContext();
        return ctx.Projects.AsNoTracking().OrderBy(p => p.Name).ToList();
    }

    public List<DistributionBoard> GetDistributionBoards(int projectId)
    {
        using var ctx = CreateContext();
        return ctx.DistributionBoards.AsNoTracking()
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.Name)
            .ToList();
    }

    public List<Room> GetRooms(int projectId)
    {
        using var ctx = CreateContext();
        return ctx.Rooms.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.Floor).ThenBy(r => r.Name)
            .ToList();
    }

    public List<CableFunction> GetFunctions()
    {
        using var ctx = CreateContext();
        return ctx.CableFunctions.AsNoTracking()
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Name)
            .ToList();
    }

    public List<CableList> GetCableLists(int projectId)
    {
        using var ctx = CreateContext();
        return ctx.CableLists.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .ToList();
    }

    public List<Cable> GetCables(int cableListId)
    {
        using var ctx = CreateContext();
        return ctx.Cables.AsNoTracking()
            .Where(c => c.CableListId == cableListId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToList();
    }

    // ---------- Kabel ----------

    /// <summary>Legt ein neues Kabel am Ende der Liste an und schlägt eine Kabel-Nr. vor.</summary>
    public Cable AddCable(int cableListId, Cable? template = null)
    {
        using var ctx = CreateContext();
        var cables = ctx.Cables.AsNoTracking().Where(c => c.CableListId == cableListId).ToList();

        var cable = new Cable
        {
            CableListId = cableListId,
            SortOrder = cables.Count == 0 ? 1 : cables.Max(c => c.SortOrder) + 1,
            Number = SuggestNextCableNumber(cables.Select(c => c.Number)),
        };

        if (template != null)
        {
            cable.Designation = template.Designation;
            cable.RoomId = template.RoomId;
            cable.FunctionId = template.FunctionId;
            cable.CableType = template.CableType;
            cable.CrossSection = template.CrossSection;
            cable.Length = template.Length;
            cable.Fuse = template.Fuse;
            cable.Terminal = template.Terminal;
            cable.Remark = template.Remark;
            cable.AdditionalText = template.AdditionalText;
        }

        ctx.Cables.Add(cable);
        ctx.SaveChanges();
        return cable;
    }

    /// <summary>
    /// Ermittelt die nächste Kabel-Nr. anhand der höchsten vorhandenen Nummer
    /// mit gleichem Präfix (z. B. W1, W2 → W3). Standard ist "W".
    /// </summary>
    public static string SuggestNextCableNumber(IEnumerable<string?> existingNumbers) =>
        SuggestNextNumber(existingNumbers, "W");

    /// <summary>Nächste fortlaufende Kennzeichnung (Präfix + Zahl) anhand der höchsten vorhandenen.</summary>
    public static string SuggestNextNumber(IEnumerable<string?> existingNumbers, string defaultPrefix)
    {
        var prefix = defaultPrefix;
        var max = 0;

        foreach (var number in existingNumbers)
        {
            if (string.IsNullOrWhiteSpace(number))
                continue;

            var trimmed = number.Trim();
            var digitStart = trimmed.Length;
            while (digitStart > 0 && char.IsDigit(trimmed[digitStart - 1]))
                digitStart--;

            if (digitStart == trimmed.Length)
                continue;

            if (int.TryParse(trimmed[digitStart..], out var value) && value >= max)
            {
                max = value;
                prefix = trimmed[..digitStart];
            }
        }

        return $"{prefix}{max + 1}";
    }

    /// <summary>Speichert die aktuelle Reihenfolge der Kabel (1, 2, 3, …).</summary>
    public void SaveCableOrder(IList<Cable> cables)
    {
        using var ctx = CreateContext();
        for (var i = 0; i < cables.Count; i++)
        {
            cables[i].SortOrder = i + 1;
            ctx.Cables.Update(cables[i]);
        }
        ctx.SaveChanges();
    }

    /// <summary>Speichert die aktuelle Reihenfolge der Funktionen.</summary>
    public void SaveFunctionOrder(IList<CableFunction> functions)
    {
        using var ctx = CreateContext();
        for (var i = 0; i < functions.Count; i++)
        {
            functions[i].SortOrder = i + 1;
            ctx.CableFunctions.Update(functions[i]);
        }
        ctx.SaveChanges();
    }

    /// <summary>Anzahl der Kabel, denen diese Funktion zugeordnet ist.</summary>
    public int CountCablesWithFunction(int functionId)
    {
        using var ctx = CreateContext();
        return ctx.Cables.Count(c => c.FunctionId == functionId);
    }

    public int CountCablesInRoom(int roomId)
    {
        using var ctx = CreateContext();
        return ctx.Cables.Count(c => c.RoomId == roomId);
    }

    public int CountCableListsOfBoard(int boardId)
    {
        using var ctx = CreateContext();
        return ctx.CableLists.Count(l => l.DistributionBoardId == boardId);
    }

    // ---------- Aktor-Vorlagen und Aktoren ----------

    public List<ActuatorTemplate> GetActuatorTemplates()
    {
        using var ctx = CreateContext();
        return ctx.ActuatorTemplates.AsNoTracking()
            .OrderBy(t => t.Mounting).ThenBy(t => t.Kind).ThenBy(t => t.Name)
            .ToList();
    }

    public int CountActuatorsWithTemplate(int templateId)
    {
        using var ctx = CreateContext();
        return ctx.Actuators.Count(a => a.TemplateId == templateId);
    }

    public List<Actuator> GetActuators(int projectId)
    {
        using var ctx = CreateContext();
        return ctx.Actuators.AsNoTracking()
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.Name)
            .ToList();
    }

    /// <summary>Schlägt die nächste Aktor-Kennzeichnung vor (A1, A2, …).</summary>
    public string SuggestNextActuatorName(int projectId)
    {
        using var ctx = CreateContext();
        var names = ctx.Actuators.AsNoTracking().Where(a => a.ProjectId == projectId).Select(a => a.Name).ToList();
        return SuggestNextNumber(names, "A");
    }

    /// <summary>Löscht einen Aktor und gibt alle ihm zugewiesenen Kanäle frei.</summary>
    public void DeleteActuator(Actuator actuator)
    {
        using var ctx = CreateContext();
        ctx.RoomFunctions.Where(r => r.ActuatorId == actuator.Id)
            .ExecuteUpdate(u => u.SetProperty(r => r.ActuatorId, (int?)null).SetProperty(r => r.Channel, (int?)null));
        ctx.Actuators.Where(a => a.Id == actuator.Id).ExecuteDelete();
    }

    public int CountRoomFunctionsOfActuator(int actuatorId)
    {
        using var ctx = CreateContext();
        return ctx.RoomFunctions.Count(r => r.ActuatorId == actuatorId);
    }

    // ---------- Raum-Funktionen ----------

    public List<RoomFunction> GetRoomFunctions(int projectId)
    {
        using var ctx = CreateContext();
        return ctx.RoomFunctions.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .ToList();
    }

    /// <summary>
    /// Legt für jede Kombination aus Raum und Funktion, die in den Kabellisten des Projekts
    /// vorkommt, eine Raum-Funktion (Kreis 1) an, falls noch keine existiert.
    /// </summary>
    /// <returns>Anzahl neu angelegter Raum-Funktionen.</returns>
    public int SyncRoomFunctionsFromCables(int projectId)
    {
        using var ctx = CreateContext();
        var combos = GetCableCombos(ctx, projectId).Select(c => (c.RoomId, c.FunctionId)).Distinct().ToList();

        var existing = ctx.RoomFunctions.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .Select(r => new { r.RoomId, r.FunctionId })
            .ToList()
            .Select(r => (r.RoomId, r.FunctionId))
            .ToHashSet();

        var added = 0;
        foreach (var combo in combos.Where(c => !existing.Contains(c)))
        {
            ctx.RoomFunctions.Add(new RoomFunction
            {
                ProjectId = projectId,
                RoomId = combo.RoomId,
                FunctionId = combo.FunctionId,
                Circuit = 1,
            });
            added++;
        }

        ctx.SaveChanges();
        return added;
    }

    /// <summary>
    /// Liefert je (Raum, Funktion) die Kabel-Nummern aus allen Kabellisten des Projekts,
    /// z. B. "W1, W4 (UV-EG)".
    /// </summary>
    public Dictionary<(int RoomId, int FunctionId), string> GetCableSummaryByRoomFunction(int projectId)
    {
        using var ctx = CreateContext();
        return GetCableCombos(ctx, projectId)
            .GroupBy(c => (c.RoomId, c.FunctionId))
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.GroupBy(c => c.BoardName)
                    .Select(b => $"{string.Join(", ", b.Select(c => c.Number))} ({b.Key})")));
    }

    private static List<(int RoomId, int FunctionId, string Number, string BoardName)> GetCableCombos(PlannerDbContext ctx, int projectId)
    {
        var query =
            from c in ctx.Cables
            join l in ctx.CableLists on c.CableListId equals l.Id
            join b in ctx.DistributionBoards on l.DistributionBoardId equals b.Id
            where l.ProjectId == projectId && c.RoomId != null && c.FunctionId != null
            orderby b.Name, c.SortOrder
            select new { RoomId = c.RoomId!.Value, FunctionId = c.FunctionId!.Value, c.Number, BoardName = b.Name };

        return query.AsNoTracking().ToList()
            .Select(x => (x.RoomId, x.FunctionId, x.Number, x.BoardName))
            .ToList();
    }

    /// <summary>Legt einen weiteren Kreis für dieselbe Raum-Funktion an (nächste Kreis-Nummer).</summary>
    public RoomFunction AddCircuit(int projectId, int roomId, int functionId)
    {
        using var ctx = CreateContext();
        var max = ctx.RoomFunctions
            .Where(r => r.ProjectId == projectId && r.RoomId == roomId && r.FunctionId == functionId)
            .Select(r => (int?)r.Circuit)
            .Max() ?? 0;

        var rf = new RoomFunction { ProjectId = projectId, RoomId = roomId, FunctionId = functionId, Circuit = max + 1 };
        ctx.RoomFunctions.Add(rf);
        ctx.SaveChanges();
        return rf;
    }
}
