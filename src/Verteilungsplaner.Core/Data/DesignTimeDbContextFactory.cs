using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Verteilungsplaner.Core.Data;

/// <summary>Wird nur von "dotnet ef migrations" benötigt.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    public PlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite("Data Source=design.db")
            .Options;
        return new PlannerDbContext(options);
    }
}
