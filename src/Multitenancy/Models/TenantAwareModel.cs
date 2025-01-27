namespace Multitenancy.Models;

public interface ITenantAware
{
    Guid TenantId { get; set; }
}

public record TenantAwareModelRecord : ITenantAware
{
    public Guid TenantId { get; set; }
}

public class TenantAwareModel : ITenantAware
{
    public Guid TenantId { get; set; }
}