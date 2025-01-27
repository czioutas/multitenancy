using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Multitenancy.Entities;
using Multitenancy.Services;

namespace Multitenancy;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IRequestTenant _requestTenant;

    public TenantSaveChangesInterceptor(IRequestTenant requestTenant)
    {
        _requestTenant = requestTenant;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return new ValueTask<InterceptionResult<int>>(result);

        var tenantId = _requestTenant.TenantId;
        foreach (var entry in context.ChangeTracker.Entries<ITenantAwareEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.TenantId = entry.Entity.TenantId == Guid.Empty
                        ? tenantId
                        : entry.Entity.TenantId;
                    break;
            }
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void ApplyTenantId(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantAwareEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Set TenantId for newly added entities if it's not already set
                    entry.Entity.TenantId = entry.Entity.TenantId == Guid.Empty
                        ? _requestTenant.TenantId
                        : entry.Entity.TenantId;
                    break;

                case EntityState.Modified:
                    // Ensure TenantId matches the current tenant
                    if (entry.Entity.TenantId != _requestTenant.TenantId)
                    {
                        entry.Entity.TenantId = _requestTenant.TenantId;
                    }
                    break;
            }
        }
    }
}