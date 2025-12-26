using SoftTrack.Entities;

namespace SoftTrack.Interfaces;

/// <summary>
/// Repository interface for managing versioned entities with soft delete support.
/// </summary>
/// <typeparam name="T">The entity type that inherits from VersionedEntity.</typeparam>
public interface ISoftTrackRepository<T> where T : VersionedEntity
{
    /// <summary>
    /// Creates a new entity with version 1.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="userId">Optional user identifier for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity.</returns>
    Task<T> CreateAsync(T entity, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity, incrementing its version.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="userId">Optional user identifier for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    Task<T> UpdateAsync(T entity, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity by setting IsDeleted to true.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="userId">Optional user identifier for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SoftDeleteAsync(Guid id, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="userId">Optional user identifier for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(Guid id, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an entity to a specific version.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="version">The version to restore to.</param>
    /// <param name="userId">Optional user identifier for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored entity.</returns>
    Task<T?> RestoreToVersionAsync(Guid id, int version, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="includeDeleted">Whether to include soft-deleted entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<T?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="includeDeleted">Whether to include soft-deleted entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of history records.</returns>
    Task<IEnumerable<EntityHistory<T>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
