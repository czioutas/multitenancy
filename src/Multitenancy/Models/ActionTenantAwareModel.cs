namespace Multitenancy.Models;

public record ActionTenantAwareModel
{
    public Guid TenantId { get; set; }
}
