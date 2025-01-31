using Microsoft.EntityFrameworkCore;
using Multitenancy.Services;
using Multitenancy.Test.Fixtures;

namespace Multitenancy.Test;

public class TestTenantIdentityDbContextSimple : TenantIdentityDbContext
{
    public TestTenantIdentityDbContextSimple(
        DbContextOptions<TestTenantIdentityDbContextSimple> options,
        IRequestTenant requestTenant,
        TimeProvider timeProvider,
        ITenantConfiguration tenantConfiguration)
        : base(options, requestTenant, timeProvider, tenantConfiguration)
    {
    }

    public virtual DbSet<DemoResourceEntity> DemoResources => Set<DemoResourceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}