namespace Multitenancy.Models;

/// <summary>
/// Base record type for models that are associated with a specific tenant in a multi-tenant system.
/// This record provides the fundamental tenant identification property required for tenant-aware operations.
/// </summary>
public record ActionTenantAwareModel
{
    /// <summary>
    /// Gets the unique identifier of the tenant that owns or is associated with this model.
    /// This property is used to enforce data isolation between different tenants.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionTenantAwareModel"/> class.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is empty.</exception>
    protected ActionTenantAwareModel(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        TenantId = tenantId;
    }
}