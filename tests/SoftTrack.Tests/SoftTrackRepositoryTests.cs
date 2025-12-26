using Microsoft.EntityFrameworkCore;
using SoftTrack.Entities;
using SoftTrack.Repositories;

namespace SoftTrack.Tests;

public class SoftTrackRepositoryTests
{
    private TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEntityWithVersion1()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test Description" };

        // Act
        var created = await repository.CreateAsync(entity, "user1");

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(1, created.Version);
        Assert.False(created.IsDeleted);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Equal("user1", created.CreatedBy);
    }

    [Fact]
    public async Task UpdateAsync_ShouldIncrementVersion()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Original" };
        var created = await repository.CreateAsync(entity, "user1");

        // Act
        created.Description = "Updated";
        var updated = await repository.UpdateAsync(created, "user2");

        // Assert
        Assert.Equal(2, updated.Version);
        Assert.Equal("Updated", updated.Description);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal("user2", updated.UpdatedBy);
        Assert.Equal("user1", updated.CreatedBy); // CreatedBy should not change
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldMarkEntityAsDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test" };
        var created = await repository.CreateAsync(entity);

        // Act
        await repository.SoftDeleteAsync(created.Id, "user1");

        // Assert - Entity should not be found with default query
        var notFound = await repository.GetAsync(created.Id);
        Assert.Null(notFound);

        // Entity should be found when including deleted
        var found = await repository.GetAsync(created.Id, includeDeleted: true);
        Assert.NotNull(found);
        Assert.True(found.IsDeleted);
        Assert.NotNull(found.DeletedAt);
        Assert.Equal("user1", found.DeletedBy);
    }

    [Fact]
    public async Task RestoreAsync_ShouldUnmarkEntityAsDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "Test" };
        var created = await repository.CreateAsync(entity);
        await repository.SoftDeleteAsync(created.Id);

        // Act
        await repository.RestoreAsync(created.Id, "user1");

        // Assert
        var restored = await repository.GetAsync(created.Id);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAt);
        Assert.Null(restored.DeletedBy);
        Assert.Equal("user1", restored.UpdatedBy);
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedByDefault()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        
        await repository.CreateAsync(new TestEntity { Name = "Entity1" });
        var entity2 = await repository.CreateAsync(new TestEntity { Name = "Entity2" });
        await repository.CreateAsync(new TestEntity { Name = "Entity3" });
        
        await repository.SoftDeleteAsync(entity2.Id);

        // Act
        var all = await repository.GetAllAsync();
        var allIncludingDeleted = await repository.GetAllAsync(includeDeleted: true);

        // Assert
        Assert.Equal(2, all.Count());
        Assert.Equal(3, allIncludingDeleted.Count());
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAllVersions()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test", Description = "V1" };
        
        var created = await repository.CreateAsync(entity, "user1");
        created.Description = "V2";
        var updated1 = await repository.UpdateAsync(created, "user1");
        updated1.Description = "V3";
        await repository.UpdateAsync(updated1, "user1");

        // Act
        var history = await repository.GetHistoryAsync(created.Id);

        // Assert
        Assert.Equal(3, history.Count());
        var historyList = history.ToList();
        Assert.Equal(1, historyList[0].Version);
        Assert.Equal(2, historyList[1].Version);
        Assert.Equal(3, historyList[2].Version);
        Assert.Equal(OperationType.Create, historyList[0].OperationType);
        Assert.Equal(OperationType.Update, historyList[1].OperationType);
        Assert.Equal(OperationType.Update, historyList[2].OperationType);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenEntityNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(entity));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenEntityIsDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test" };
        var created = await repository.CreateAsync(entity);
        await repository.SoftDeleteAsync(created.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(created));
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldThrowWhenEntityNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SoftDeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RestoreAsync_ShouldThrowWhenEntityNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new SoftTrackRepository<TestEntity>(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RestoreAsync(Guid.NewGuid()));
    }
}
