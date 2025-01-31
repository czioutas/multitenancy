using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Multitenancy;
using Multitenancy.Data;
using Multitenancy.Services;

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

    /// <summary>
    /// Gets the time provider used for automatic timestamp updates.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Gets the tenant configuration used to customize multi-tenancy behavior.
    /// </summary>
    private readonly ITenantConfiguration _tenantConfiguration;

    /// <summary>
    /// Initializes a new instance of the TenantIdentityDbContext class.
    /// </summary>
    /// <param name="options">The options to be used by a DbContext.</param>
    /// <param name="requestTenant">The service that provides the current tenant context.</param>
    /// <param name="timeProvider">The provider used for timestamp operations.</param>
    /// <param name="tenantConfiguration">The configuration for customizing multi-tenancy behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown when requestTenant, timeProvider, or tenantConfiguration is null.</exception>
    protected TenantIdentityDbContext(DbContextOptions options, IRequestTenant requestTenant, TimeProvider timeProvider, ITenantConfiguration tenantConfiguration) : base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tenantConfiguration = tenantConfiguration ?? throw new ArgumentNullException(nameof(tenantConfiguration));
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

        if (_tenantConfiguration.WithTenantEntity)
        {
            modelBuilder.AddTenantEntity();
        }

        modelBuilder.ApplyTenantFilters(_requestTenant);
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
        this.UpdateTenantStatuses(_requestTenant, _timeProvider);
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        this.UpdateTenantStatuses(_requestTenant, _timeProvider);
        return base.SaveChanges();
    }
}

/// <summary>
/// Represents a DbContext that integrates Identity with multi-tenancy support.
/// This class extends the standard IdentityDbContext to provide automatic tenant isolation
/// and tracking for entities that implement ITenantAwareEntity.
/// </summary>
/// <remarks>
/// This context automatically applies tenant filtering to all entities implementing ITenantAwareEntity,
/// ensuring data isolation between different tenants. It also handles the automatic setting of tenant IDs
/// and timestamp updates for tenant-aware entities during save operations.
/// </remarks>
public abstract class TenantIdentityDbContext : IdentityDbContext
{

    /// <summary>
    /// Gets the request tenant service used for tenant isolation.
    /// </summary>
    protected readonly IRequestTenant _requestTenant;

    /// <summary>
    /// Gets the time provider used for automatic timestamp updates.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Gets the tenant configuration used to customize multi-tenancy behavior.
    /// </summary>
    private readonly ITenantConfiguration _tenantConfiguration;

    /// <summary>
    /// Initializes a new instance of the TenantIdentityDbContext class.
    /// </summary>
    /// <param name="options">The options to be used by a DbContext.</param>
    /// <param name="requestTenant">The service that provides the current tenant context.</param>
    /// <param name="timeProvider">The provider used for timestamp operations.</param>
    /// <param name="tenantConfiguration">The configuration for customizing multi-tenancy behavior.</param>
    /// <exception cref="ArgumentNullException">Thrown when requestTenant, timeProvider, or tenantConfiguration is null.</exception>
    protected TenantIdentityDbContext(DbContextOptions options, IRequestTenant requestTenant, TimeProvider timeProvider, ITenantConfiguration tenantConfiguration) : base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tenantConfiguration = tenantConfiguration ?? throw new ArgumentNullException(nameof(tenantConfiguration));
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

        if (_tenantConfiguration.WithTenantEntity)
        {
            modelBuilder.AddTenantEntity();
        }

        modelBuilder.ApplyTenantFilters(_requestTenant);
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
        this.UpdateTenantStatuses(_requestTenant, _timeProvider);
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        this.UpdateTenantStatuses(_requestTenant, _timeProvider);
        return base.SaveChanges();
    }
}
