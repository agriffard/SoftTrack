using Microsoft.EntityFrameworkCore;
using SoftTrack.Entities;

namespace SoftTrack.Tests;

/// <summary>
/// Test DbContext for testing SoftTrack functionality.
/// </summary>
public class TestDbContext : SoftTrackDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<EntityHistory<TestEntity>> TestEntityHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EntityHistory<TestEntity>>()
            .HasKey(h => h.Id);

        modelBuilder.Entity<EntityHistory<TestEntity>>()
            .HasIndex(h => new { h.EntityId, h.Version });
    }
}
