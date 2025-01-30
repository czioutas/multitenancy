using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Multitenancy.Services;
using Multitenancy.Test.Fixtures;
using MultiTenancy.Data;

namespace Multitenancy.Test;

public class TestDbContext : TenantIdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        IRequestTenant requestTenant,
        TimeProvider timeProvider)
        : base(options, requestTenant, timeProvider)
    {
    }

    public virtual DbSet<DemoResourceEntity> DemoResources => Set<DemoResourceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}