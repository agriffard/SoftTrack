using SoftTrack.Entities;

namespace SoftTrack.Tests;

/// <summary>
/// Test entity for testing SoftTrack functionality.
/// </summary>
public class TestEntity : VersionedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
