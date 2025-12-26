using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftTrack.Entities;
using SoftTrack.Interfaces;
using SoftTrack.Repositories;

namespace SoftTrack.Extensions;

/// <summary>
/// Extension methods for configuring SoftTrack services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SoftTrack repository services for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSoftTrackRepository<TEntity, TContext>(this IServiceCollection services)
        where TEntity : VersionedEntity
        where TContext : DbContext
    {
        services.AddScoped<ISoftTrackRepository<TEntity>>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new SoftTrackRepository<TEntity>(context);
        });

        return services;
    }

    /// <summary>
    /// Adds generic SoftTrack repository services.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSoftTrack<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped(typeof(ISoftTrackRepository<>), typeof(SoftTrackRepository<>));
        return services;
    }
}
