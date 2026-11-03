using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Verteilungsplaner.Core.Data;
using Verteilungsplaner.Core.Models;
using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.Tests;

public class UpgradeTests
{
    [Fact]
    public void DatabaseFromVersion01_IsUpgraded_WithoutDuplicateFunctions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vp-upgrade-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<PlannerDbContext>().UseSqlite($"Data Source={path}").Options;
        try
        {
            // Stand von Version 0.1: nur erste Migration, Funktionen bereits vorhanden
            using (var ctx = new PlannerDbContext(options))
            {
                ctx.GetService<IMigrator>().Migrate("20261103164631_InitialCreate");
                ctx.Database.ExecuteSqlRaw("INSERT INTO CableFunctions (Name, SortOrder) VALUES ('Beleuchtung', 1), ('Eigene', 2)");
            }

            var service = new PlannerDataService(options);
            service.Initialize();

            var functions = service.GetFunctions();
            Assert.Equal(2, functions.Count);
            Assert.NotEmpty(service.GetActuatorTemplates());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}
