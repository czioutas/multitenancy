using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Multitenancy.Entities;
using Multitenancy.Services;

namespace MultiTenancy.Data;

/// <summary>
/// Represents a DbContext that integrates Identity with multi-tenancy support.
/// This class extends the standard IdentityDbContext to provide automatic tenant isolation
/// and tracking for entities that implement ITenantAwareEntity.
/// </summary>
/// <typeparam name="TUser">The type representing a user in the Identity system. Must inherit from IdentityUser{TKey}.</typeparam>
/// <typeparam name="TRole">The type representing a role in the Identity system. Must inherit from IdentityRole{TKey}.</typeparam>
/// <typeparam name="TKey">The type of the primary key used for users and roles in the Identity system.</typeparam>
/// <remarks>
/// This context automatically applies tenant filtering to all entities implementing ITenantAwareEntity,
/// ensuring data isolation between different tenants. It also handles the automatic setting of tenant IDs
/// and timestamp updates for tenant-aware entities during save operations.
/// </remarks>
public abstract class TenantIdentityDbContext<TUser, TRole, TKey> :
   IdentityDbContext<TUser, TRole, TKey>
   where TUser : IdentityUser<TKey>
   where TRole : IdentityRole<TKey>
   where TKey : IEquatable<TKey>
{

    /// <summary>
    /// Gets the request tenant service used for tenant isolation.
    /// </summary>
    protected readonly IRequestTenant _requestTenant;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Gets the time provider used for automatic timestamp updates.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the TenantIdentityDbContext class.
    /// </summary>
    /// <param name="options">The options to be used by a DbContext.</param>
    /// <param name="requestTenant">The service that provides the current tenant context.</param>
    /// <param name="timeProvider">The provider used for timestamp operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when requestTenant or timeProvider is null.</exception>
    protected TenantIdentityDbContext(DbContextOptions options, IRequestTenant requestTenant, TimeProvider timeProvider) : base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types
    /// exposed in Microsoft.EntityFrameworkCore.DbSet`1 properties on your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    /// <remarks>
    /// This method applies tenant isolation filters to all entities implementing ITenantAwareEntity
    /// and configures unique constraints for tenant identifiers.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantEntity>()
            .HasIndex(t => t.Identifier)
            .IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType))
            {

                if (typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var tenantProperty = Expression.Property(parameter, "TenantId");
                    var tenantValue = Expression.Property(Expression.Field(Expression.Constant(this), "_requestTenant"), "TenantId");

                    var filter = Expression.Lambda(
                        Expression.Equal(tenantProperty, tenantValue),
                        parameter
                    );

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }
    }

    /// <summary>
    /// Saves all changes made in this context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains
    /// the number of state entries written to the database.
    /// </returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateStatuses();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        UpdateStatuses();
        return base.SaveChanges();
    }

    /// <summary>
    /// Updates tenant IDs and timestamps for tracked entities before saving changes.
    /// </summary>
    /// <remarks>
    /// This method:
    /// - Sets the TenantId for new or modified TenantAwareEntity instances
    /// - Updates CreatedAt and UpdatedAt timestamps for TenantEntity instances
    /// </remarks>
    private void UpdateStatuses()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantAwareEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty
                    ? entry.Entity.TenantId
                    : _requestTenant.TenantId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = _timeProvider.GetUtcNow();
                entry.Entity.UpdatedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = _timeProvider.GetUtcNow();
            }
        }
    }
}
