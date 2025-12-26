using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoftTrack.Entities;
using SoftTrack.Interfaces;

namespace SoftTrack.Repositories;

/// <summary>
/// Repository implementation for managing versioned entities with soft delete support.
/// </summary>
/// <typeparam name="T">The entity type that inherits from VersionedEntity.</typeparam>
public class SoftTrackRepository<T> : ISoftTrackRepository<T> where T : VersionedEntity
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftTrackRepository{T}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SoftTrackRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc/>
    public async Task<T> CreateAsync(T entity, string? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.Version = 1;
        entity.IsDeleted = false;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = userId;
        entity.UpdatedAt = null;
        entity.DeletedAt = null;
        entity.DeletedBy = null;

        _dbSet.Add(entity);

        // Add history record in the same transaction
        AddHistoryRecord(entity, OperationType.Create, userId);

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <inheritdoc/>
    public async Task<T> UpdateAsync(T entity, string? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var existingEntity = await _dbSet.FindAsync([entity.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Entity with Id {entity.Id} not found.");

        if (existingEntity.IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a deleted entity.");
        }

        // Preserve original creation values before copying
        var originalCreatedAt = existingEntity.CreatedAt;
        var originalCreatedBy = existingEntity.CreatedBy;

        // Copy new values
        var entry = _context.Entry(existingEntity);
        entry.CurrentValues.SetValues(entity);

        // Restore preserved values and set update tracking
        existingEntity.Version++;
        existingEntity.UpdatedAt = DateTime.UtcNow;
        existingEntity.UpdatedBy = userId;
        existingEntity.CreatedAt = originalCreatedAt;
        existingEntity.CreatedBy = originalCreatedBy;

        // Mark creation fields as unmodified to prevent EF from changing them
        entry.Property(nameof(VersionedEntity.CreatedAt)).IsModified = false;
        entry.Property(nameof(VersionedEntity.CreatedBy)).IsModified = false;

        // Add history record in the same transaction
        AddHistoryRecord(existingEntity, OperationType.Update, userId);

        await _context.SaveChangesAsync(cancellationToken);

        return existingEntity;
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(Guid id, string? userId = null, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Entity with Id {id} not found.");

        if (entity.IsDeleted)
        {
            return; // Already deleted
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = userId;
        entity.Version++;

        // Add history record in the same transaction
        AddHistoryRecord(entity, OperationType.SoftDelete, userId);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(Guid id, string? userId = null, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Entity with Id {id} not found.");

        if (!entity.IsDeleted)
        {
            return; // Not deleted
        }

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.Version++;

        // Add history record in the same transaction
        AddHistoryRecord(entity, OperationType.Restore, userId);

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<T?> RestoreToVersionAsync(Guid id, int version, string? userId = null, CancellationToken cancellationToken = default)
    {
        var historyDbSet = _context.Set<EntityHistory<T>>();
        var historyRecord = await historyDbSet
            .Where(h => h.EntityId == id && h.Version == version)
            .FirstOrDefaultAsync(cancellationToken);

        if (historyRecord == null)
        {
            return null;
        }

        var restoredEntity = JsonSerializer.Deserialize<T>(historyRecord.Data);
        if (restoredEntity == null)
        {
            return null;
        }

        var existingEntity = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (existingEntity == null)
        {
            return null;
        }

        var entry = _context.Entry(existingEntity);
        entry.CurrentValues.SetValues(restoredEntity);

        existingEntity.Version = existingEntity.Version + 1;
        existingEntity.UpdatedAt = DateTime.UtcNow;
        existingEntity.UpdatedBy = userId;
        existingEntity.IsDeleted = false;
        existingEntity.DeletedAt = null;
        existingEntity.DeletedBy = null;

        // Add history record in the same transaction
        AddHistoryRecord(existingEntity, OperationType.Restore, userId);

        await _context.SaveChangesAsync(cancellationToken);

        return existingEntity;
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (includeDeleted)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        // Use FirstOrDefaultAsync instead of FindAsync to apply query filters
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (includeDeleted)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);
        }

        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EntityHistory<T>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var historyDbSet = _context.Set<EntityHistory<T>>();
        return await historyDbSet
            .Where(h => h.EntityId == id)
            .OrderBy(h => h.Version)
            .ToListAsync(cancellationToken);
    }

    private void AddHistoryRecord(T entity, OperationType operationType, string? userId)
    {
        try
        {
            var historyDbSet = _context.Set<EntityHistory<T>>();
            var history = new EntityHistory<T>
            {
                Id = Guid.NewGuid(),
                EntityId = entity.Id,
                Version = entity.Version,
                Data = JsonSerializer.Serialize(entity),
                OperationType = operationType,
                CreatedAt = DateTime.UtcNow,
                PerformedBy = userId
            };

            historyDbSet.Add(history);
        }
        catch (InvalidOperationException)
        {
            // History DbSet not configured - skip history tracking
        }
    }
}
