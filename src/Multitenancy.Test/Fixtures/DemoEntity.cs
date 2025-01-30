using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Multitenancy.Entities;

namespace Multitenancy.Test.Fixtures;

public class DemoResourceEntity : ITenantAwareEntity
{
    [Key, Column("Id")]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid TenantId { get; set; }
    public TenantEntity? Tenant { get; set; }
}