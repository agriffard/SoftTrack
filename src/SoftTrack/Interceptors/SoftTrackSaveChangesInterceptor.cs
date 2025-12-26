using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoftTrack.Entities;

namespace SoftTrack.Interceptors;

/// <summary>
/// EF Core interceptor that intercepts save changes to apply soft delete and versioning logic.
/// </summary>
public class SoftTrackSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly string? _userId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftTrackSaveChangesInterceptor"/> class.
    /// </summary>
    /// <param name="userId">Optional user identifier for audit.</param>
    public SoftTrackSaveChangesInterceptor(string? userId = null)
    {
        _userId = userId;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context != null)
        {
            ApplySoftTrackChanges(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            ApplySoftTrackChanges(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftTrackChanges(DbContext context)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<VersionedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.NewGuid();
                    }
                    entry.Entity.Version = 1;
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= _userId;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.Version++;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= _userId;
                    
                    // Prevent modification of creation tracking fields
                    entry.Property(nameof(VersionedEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(VersionedEntity.CreatedBy)).IsModified = false;
                    break;

                case EntityState.Deleted:
                    // Convert hard delete to soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy ??= _userId;
                    entry.Entity.Version++;
                    break;
            }
        }
    }
}
