using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Multitenancy;
using Multitenancy.Entities;
using Multitenancy.Services;

namespace MultiTenancy.Data;

public abstract class TenantIdentityDbContext<TUser, TRole, TKey> :
   IdentityDbContext<TUser, TRole, TKey>
   where TUser : IdentityUser<TKey>
   where TRole : IdentityRole<TKey>
   where TKey : IEquatable<TKey>
{
    protected readonly IRequestTenant _requestTenant;

    protected TenantIdentityDbContext(DbContextOptions options, IRequestTenant requestTenant) : base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantEntity>()
            .HasIndex(t => t.Identifier)
            .IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType))
            {

                if (typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var tenantProperty = Expression.Property(parameter, "TenantId");
                    var tenantValue = Expression.Property(Expression.Field(Expression.Constant(this), "_requestTenant"), "TenantId");

                    var filter = Expression.Lambda(
                        Expression.Equal(tenantProperty, tenantValue),
                        parameter
                    );

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateStatuses();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateStatuses();
        return base.SaveChanges();
    }

    private void UpdateStatuses()
    {
        foreach (var entry in ChangeTracker.Entries<TenantAwareEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty ? entry.Entity.TenantId : _requestTenant.TenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty ? entry.Entity.TenantId : _requestTenant.TenantId;
                    break;
            }
        }
    }
}
