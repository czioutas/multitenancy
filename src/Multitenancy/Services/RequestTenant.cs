namespace Multitenancy.Services;

/// <summary>
/// Defines a contract for accessing and managing the tenant ID for the current request in a multi-tenant system.
/// </summary>
public interface IRequestTenant
{
    /// <summary>
    /// Gets the unique identifier of the tenant associated with the current request.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Sets the tenant ID for the current request.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to associate with the current request.</param>
    void SetTenantId(Guid tenantId);
}

/// <summary>
/// Implements the IRequestTenant interface to manage tenant context for the current request.
/// </summary>
public class RequestTenant : IRequestTenant
{
    /// <summary>
    /// Gets the unique identifier of the tenant associated with the current request.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the RequestTenant class.
    /// </summary>
    public RequestTenant()
    {
    }

    /// <summary>
    /// Sets the tenant ID for the current request.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to associate with the current request.</param>
    void IRequestTenant.SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
