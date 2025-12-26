# SoftTrack

A .NET library for managing entities with **Soft Delete** and **Version Tracking** support for Entity Framework Core.

## Features

- **Soft Delete**: Mark entities as deleted without physically removing them from the database
- **Version Tracking**: Automatically track version numbers for each entity modification
- **Audit Trail**: Record who created, updated, or deleted entities and when
- **History**: Keep a complete history of entity changes
- **Query Filters**: Automatically filter out soft-deleted entities in queries
- **Restore**: Restore soft-deleted entities or roll back to previous versions
- **EF Core Integration**: Seamless integration with Entity Framework Core

## Installation

```bash
dotnet add package SoftTrack
```

## Quick Start

### 1. Define your entity

Inherit from `VersionedEntity` to enable soft delete and versioning:

```csharp
using SoftTrack.Entities;

public class Product : VersionedEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### 2. Configure your DbContext

Inherit from `SoftTrackDbContext` to automatically apply soft delete query filters:

```csharp
using Microsoft.EntityFrameworkCore;
using SoftTrack;
using SoftTrack.Entities;

public class AppDbContext : SoftTrackDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    
    // Optional: Configure history tracking
    public DbSet<EntityHistory<Product>> ProductHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure history table
        modelBuilder.Entity<EntityHistory<Product>>()
            .HasKey(h => h.Id);
    }
}
```

### 3. Register services

```csharp
using SoftTrack.Extensions;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register repository for specific entity
builder.Services.AddSoftTrackRepository<Product, AppDbContext>();

// Or register generic repository support
builder.Services.AddSoftTrack<AppDbContext>();
```

### 4. Use the repository

```csharp
using SoftTrack.Interfaces;

public class ProductService
{
    private readonly ISoftTrackRepository<Product> _repository;

    public ProductService(ISoftTrackRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Product> CreateProduct(string name, decimal price, string userId)
    {
        var product = new Product { Name = name, Price = price };
        return await _repository.CreateAsync(product, userId);
    }

    public async Task UpdateProduct(Product product, string userId)
    {
        await _repository.UpdateAsync(product, userId);
    }

    public async Task DeleteProduct(Guid id, string userId)
    {
        await _repository.SoftDeleteAsync(id, userId);
    }

    public async Task RestoreProduct(Guid id, string userId)
    {
        await _repository.RestoreAsync(id, userId);
    }

    public async Task<IEnumerable<EntityHistory<Product>>> GetProductHistory(Guid id)
    {
        return await _repository.GetHistoryAsync(id);
    }
}
```

## Using the Interceptor

For automatic soft delete and versioning on `SaveChanges`, use the `SoftTrackSaveChangesInterceptor`:

```csharp
using SoftTrack.Interceptors;

var interceptor = new SoftTrackSaveChangesInterceptor(userId: "system");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(interceptor));
```

With the interceptor:
- **Adding** an entity automatically sets `Version = 1`, `CreatedAt`, and `CreatedBy`
- **Modifying** an entity automatically increments `Version` and sets `UpdatedAt` and `UpdatedBy`
- **Deleting** an entity is converted to a soft delete (sets `IsDeleted = true`, `DeletedAt`, `DeletedBy`)

## Entity Properties

`VersionedEntity` provides the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Version` | `int` | Version number (starts at 1) |
| `IsDeleted` | `bool` | Soft delete flag |
| `CreatedAt` | `DateTime` | Creation timestamp |
| `UpdatedAt` | `DateTime?` | Last update timestamp |
| `DeletedAt` | `DateTime?` | Deletion timestamp |
| `CreatedBy` | `string?` | Creator user ID |
| `UpdatedBy` | `string?` | Last updater user ID |
| `DeletedBy` | `string?` | Deleter user ID |

## API Reference

### ISoftTrackRepository<T>

| Method | Description |
|--------|-------------|
| `CreateAsync(entity, userId)` | Create a new entity with version 1 |
| `UpdateAsync(entity, userId)` | Update entity and increment version |
| `SoftDeleteAsync(id, userId)` | Mark entity as deleted |
| `RestoreAsync(id, userId)` | Restore a soft-deleted entity |
| `RestoreToVersionAsync(id, version, userId)` | Restore entity to specific version |
| `GetAsync(id, includeDeleted)` | Get entity by ID |
| `GetAllAsync(includeDeleted)` | Get all entities |
| `GetHistoryAsync(id)` | Get entity version history |

## License

MIT