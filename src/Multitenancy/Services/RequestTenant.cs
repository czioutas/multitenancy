namespace Multitenancy.Services;

public interface IRequestTenant
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}

public class RequestTenant : IRequestTenant
{
    public Guid TenantId { get; private set; }

    public RequestTenant()
    {
    }

    void IRequestTenant.SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
