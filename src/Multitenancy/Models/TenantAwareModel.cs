namespace Multitenancy.Models;

/// <summary>
/// Defines a contract for entities that are associated with a specific tenant in a multi-tenant system.
/// </summary>
public interface ITenantAware
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant that owns or is associated with this entity.
    /// This property is used to enforce data isolation between different tenants.
    /// </summary>
    Guid TenantId { get; set; }
}

/// <summary>
/// Base record type for models that are associated with a specific tenant in a multi-tenant system.
/// This record provides the fundamental tenant identification property required for tenant-aware operations.
/// </summary>
public record TenantAwareModelRecord : ITenantAware
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant that owns or is associated with this model.
    /// This property is used to enforce data isolation between different tenants.
    /// </summary>
    public Guid TenantId { get; set; }
}

/// <summary>
/// Base class for models that are associated with a specific tenant in a multi-tenant system.
/// This class provides the fundamental tenant identification property required for tenant-aware operations.
/// </summary>
public class TenantAwareModel : ITenantAware
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant that owns or is associated with this model.
    /// This property is used to enforce data isolation between different tenants.
    /// </summary>
    public Guid TenantId { get; set; }
}