using Microsoft.EntityFrameworkCore;
using SoftTrack.Interceptors;

namespace SoftTrack.Tests;

public class SoftTrackInterceptorTests
{
    private TestDbContext CreateContextWithInterceptor(string? userId = null)
    {
        var interceptor = new SoftTrackSaveChangesInterceptor(userId);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task Interceptor_ShouldSetVersionOnAdd()
    {
        // Arrange
        using var context = CreateContextWithInterceptor("testUser");
        var entity = new TestEntity { Name = "Test" };

        // Act
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(1, entity.Version);
        Assert.Equal("testUser", entity.CreatedBy);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task Interceptor_ShouldIncrementVersionOnUpdate()
    {
        // Arrange
        using var context = CreateContextWithInterceptor("user1");
        var entity = new TestEntity { Name = "Test" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "Updated";
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, entity.Version);
        Assert.NotNull(entity.UpdatedAt);
    }

    [Fact]
    public async Task Interceptor_ShouldConvertDeleteToSoftDelete()
    {
        // Arrange
        using var context = CreateContextWithInterceptor("user1");
        var entity = new TestEntity { Name = "Test" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var entityId = entity.Id;

        // Act - Remove the entity (should trigger soft delete)
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Entity should still exist but be marked as deleted
        var deletedEntity = await context.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId);

        Assert.NotNull(deletedEntity);
        Assert.True(deletedEntity.IsDeleted);
        Assert.NotNull(deletedEntity.DeletedAt);
        Assert.Equal("user1", deletedEntity.DeletedBy);
    }

    [Fact]
    public async Task Interceptor_ShouldNotModifyCreatedAtOnUpdate()
    {
        // Arrange
        using var context = CreateContextWithInterceptor();
        var entity = new TestEntity { Name = "Test" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;

        // Act - Wait a bit and update
        await Task.Delay(10);
        entity.Name = "Updated";
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(originalCreatedAt, entity.CreatedAt);
    }

    [Fact]
    public async Task Interceptor_ShouldGenerateIdIfEmpty()
    {
        // Arrange
        using var context = CreateContextWithInterceptor();
        var entity = new TestEntity { Id = Guid.Empty, Name = "Test" };

        // Act
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public async Task Interceptor_ShouldPreserveProvidedId()
    {
        // Arrange
        using var context = CreateContextWithInterceptor();
        var providedId = Guid.NewGuid();
        var entity = new TestEntity { Id = providedId, Name = "Test" };

        // Act
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(providedId, entity.Id);
    }
}
