using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Multitenancy.Services;
using Multitenancy.Test.Fixtures;

namespace Multitenancy.Test;

public class TestTenantIdentityDbContext : TenantIdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public TestTenantIdentityDbContext(
        DbContextOptions<TestTenantIdentityDbContext> options,
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