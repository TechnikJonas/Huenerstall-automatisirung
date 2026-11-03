using Verteilungsplaner.Core.Services;

namespace Verteilungsplaner.Tests;

/// <summary>Temporäre SQLite-Datenbankdatei je Test.</summary>
public sealed class TestDatabase : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"vp-test-{Guid.NewGuid():N}.db");

    public PlannerDataService Service { get; }

    public TestDatabase()
    {
        Service = new PlannerDataService(Path);
        Service.Initialize();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(Path))
            File.Delete(Path);
    }
}
