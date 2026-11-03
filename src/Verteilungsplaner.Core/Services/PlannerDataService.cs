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

    private PlannerDbContext CreateContext() => new(_options);

    /// <summary>Legt die Datenbank an bzw. aktualisiert sie und befüllt Standard-Funktionen.</summary>
    public void Initialize()
    {
        using var ctx = CreateContext();
        ctx.Database.Migrate();

        if (!ctx.CableFunctions.Any())
        {
            var order = 0;
            foreach (var name in DefaultFunctions)
                ctx.CableFunctions.Add(new CableFunction { Name = name, SortOrder = ++order });
            ctx.SaveChanges();
        }
    }

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
    public static string SuggestNextCableNumber(IEnumerable<string?> existingNumbers)
    {
        var prefix = "W";
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
}
