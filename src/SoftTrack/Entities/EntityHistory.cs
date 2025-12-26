namespace SoftTrack.Entities;

/// <summary>
/// Represents a historical snapshot of an entity version.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public class EntityHistory<T> where T : VersionedEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets the version number of this history record.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the serialized JSON data of the entity at this version.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of operation that created this history record.
    /// </summary>
    public OperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this history record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the operation.
    /// </summary>
    public string? PerformedBy { get; set; }
}

/// <summary>
/// Represents the type of operation performed on an entity.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Entity was created.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Entity was updated.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Entity was soft deleted.
    /// </summary>
    SoftDelete = 3,

    /// <summary>
    /// Entity was restored from soft delete.
    /// </summary>
    Restore = 4
}
