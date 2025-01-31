using Microsoft.EntityFrameworkCore;
using Multitenancy.Entities;
using Multitenancy.Services;

namespace Multitenancy.Data;

/// <summary>
/// Provides extension methods for DbContext to handle tenant-specific operations.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Updates tenant-related statuses and timestamps for entities being tracked by the DbContext.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    /// <param name="requestTenant">The service providing the current tenant context.</param>
    /// <param name="timeProvider">The provider for timestamp operations.</param>
    /// <remarks>
    /// This method performs two main operations:
    /// 1. Sets the TenantId for ITenantAwareEntity entries that are being added or modified
    /// 2. Updates creation and modification timestamps for TenantEntity entries
    /// </remarks>
    public static void UpdateTenantStatuses(this DbContext context, IRequestTenant requestTenant, TimeProvider timeProvider)
    {
        foreach (var entry in context.ChangeTracker.Entries<ITenantAwareEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty
                    ? entry.Entity.TenantId
                    : requestTenant.TenantId;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timeProvider.GetUtcNow();
                entry.Entity.UpdatedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timeProvider.GetUtcNow();
            }
        }
    }
}