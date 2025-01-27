using System.ComponentModel.DataAnnotations.Schema;

namespace Multitenancy.Entities;

public interface ITenantAwareEntity
{
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public TenantEntity? Tenant { get; set; }
}

public class TenantAwareEntity : ITenantAwareEntity
{
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }

    public TenantEntity? Tenant { get; set; }
}