using Multitenancy.Entities;

namespace Multitenancy.Test.Fixtures;

public class DemoResourceEntity : ITenantAwareEntity
{
    public required string Name { get; set; }
    public Guid TenantId { get; set; }
    public TenantEntity? Tenant { get; set; }
}