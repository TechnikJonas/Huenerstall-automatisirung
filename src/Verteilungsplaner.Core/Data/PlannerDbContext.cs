using Microsoft.EntityFrameworkCore;
using Verteilungsplaner.Core.Models;

namespace Verteilungsplaner.Core.Data;

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<DistributionBoard> DistributionBoards => Set<DistributionBoard>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<CableFunction> CableFunctions => Set<CableFunction>();
    public DbSet<CableList> CableLists => Set<CableList>();
    public DbSet<Cable> Cables => Set<Cable>();
    public DbSet<ActuatorTemplate> ActuatorTemplates => Set<ActuatorTemplate>();
    public DbSet<Actuator> Actuators => Set<Actuator>();
    public DbSet<RoomFunction> RoomFunctions => Set<RoomFunction>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(e =>
        {
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<DistributionBoard>(e =>
        {
            e.Property(b => b.Name).IsRequired().HasMaxLength(100);
            e.HasOne<Project>().WithMany().HasForeignKey(b => b.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CableFunction>(e =>
        {
            e.Property(f => f.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<CableList>(e =>
        {
            e.Property(l => l.Name).IsRequired().HasMaxLength(200);
            e.HasOne<Project>().WithMany().HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DistributionBoard>().WithMany().HasForeignKey(l => l.DistributionBoardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cable>(e =>
        {
            e.Property(c => c.Number).HasMaxLength(50);
            e.HasOne<CableList>().WithMany().HasForeignKey(c => c.CableListId).OnDelete(DeleteBehavior.Cascade);
            // Wird ein Raum oder eine Funktion gelöscht, bleibt das Kabel erhalten – nur die Zuordnung entfällt.
            e.HasOne<Room>().WithMany().HasForeignKey(c => c.RoomId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<CableFunction>().WithMany().HasForeignKey(c => c.FunctionId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(c => new { c.CableListId, c.SortOrder });
        });

        modelBuilder.Entity<ActuatorTemplate>(e =>
        {
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Actuator>(e =>
        {
            e.Property(a => a.Name).IsRequired().HasMaxLength(100);
            e.HasOne<Project>().WithMany().HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Cascade);
            // Eine Vorlage, die noch verwendet wird, darf nicht gelöscht werden.
            e.HasOne<ActuatorTemplate>().WithMany().HasForeignKey(a => a.TemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<DistributionBoard>().WithMany().HasForeignKey(a => a.DistributionBoardId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Room>().WithMany().HasForeignKey(a => a.RoomId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RoomFunction>(e =>
        {
            e.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Room>().WithMany().HasForeignKey(r => r.RoomId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<CableFunction>().WithMany().HasForeignKey(r => r.FunctionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Actuator>().WithMany().HasForeignKey(r => r.ActuatorId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasKey(s => s.Key);
        });
    }
}
