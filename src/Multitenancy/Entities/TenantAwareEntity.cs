using System.ComponentModel.DataAnnotations.Schema;

namespace Multitenancy.Entities;

/// <summary>
/// Defines the contract for entities that support multi-tenancy.
/// Implementing this interface enables automatic tenant isolation and filtering
/// in the database context.
/// </summary>
/// <remarks>
/// Entities implementing this interface will automatically:
/// - Have their TenantId set during save operations
/// - Be filtered by the current tenant's ID in queries
/// - Maintain a relationship with their parent TenantEntity
/// </remarks>
public interface ITenantAwareEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant that owns this entity.
    /// </summary>
    /// <remarks>
    /// This property is automatically managed by the database context and
    /// should not be manually set in most cases. It's used for enforcing
    /// data isolation between tenants.
    /// </remarks>
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the tenant entity associated with this entity.
    /// Represents the parent-child relationship between the tenant and this entity.
    /// </summary>
    /// <remarks>
    /// This navigation property allows for eager or lazy loading of the parent tenant
    /// information when needed. The relationship is managed by Entity Framework Core
    /// using the TenantId foreign key.
    /// </remarks>
    public TenantEntity? Tenant { get; set; }
}

/// <summary>
/// Provides a base implementation of the ITenantAwareEntity interface.
/// This class can be inherited by domain entities that need multi-tenancy support.
/// </summary>
/// <remarks>
/// Use this class as a base class for entities that need tenant isolation.
/// It provides the standard implementation of tenant tracking properties
/// and ensures consistent tenant handling across the application.
/// </remarks>
/// <example>
/// <code>
/// public class CustomerEntity : TenantAwareEntity
/// {
///     public string Name { get; set; }
///     // Additional entity properties...
/// }
/// </code>
/// </example>
public class TenantAwareEntity : ITenantAwareEntity
{
    /// <inheritdoc/>
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }

    /// <inheritdoc/>
    public TenantEntity? Tenant { get; set; }
}