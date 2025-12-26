using Microsoft.EntityFrameworkCore;
using SoftTrack.Entities;

namespace SoftTrack;

/// <summary>
/// Base DbContext with soft delete and versioning support.
/// </summary>
public abstract class SoftTrackDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoftTrackDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    protected SoftTrackDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model to add global query filters for soft delete.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Applies soft delete query filters to all entities that inherit from VersionedEntity.
    /// Override this method to customize the filter behavior.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected virtual void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(VersionedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SoftTrackDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, [modelBuilder]);
            }
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : VersionedEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TEntity>().HasIndex(e => e.IsDeleted);
        modelBuilder.Entity<TEntity>().HasIndex(e => e.Version);
        modelBuilder.Entity<TEntity>().HasIndex(e => e.DeletedAt);
    }
}
