using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Multitenancy.Entities;
using Multitenancy.Services;

namespace Multitenancy.Data;

/// <summary>
/// Provides extension methods for ModelBuilder to configure tenant-specific behaviors.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies global query filters to ensure tenant data isolation for all entities implementing ITenantAwareEntity.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance being extended.</param>
    /// <param name="requestTenant">The service providing the current tenant context.</param>
    /// <remarks>
    /// This method automatically adds query filters to all entities that implement ITenantAwareEntity,
    /// ensuring that queries only return data belonging to the current tenant.
    /// The filters are applied globally and will affect all queries unless explicitly ignored.
    /// </remarks>
    public static void ApplyTenantFilters(this ModelBuilder modelBuilder, IRequestTenant requestTenant)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var tenantProperty = Expression.Property(parameter, "TenantId");
                var tenantValue = Expression.Constant(requestTenant.TenantId);

                var filter = Expression.Lambda(
                    Expression.Equal(tenantProperty, tenantValue),
                    parameter
                );

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    /// <summary>
    /// Configures the TenantEntity in the database model with required indexes and constraints.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance being extended.</param>
    /// <remarks>
    /// This method adds a unique index on the Identifier property of the TenantEntity,
    /// ensuring that tenant identifiers remain unique across the system.
    /// </remarks>
    public static void AddTenantEntity(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantEntity>()
            .HasIndex(t => t.Identifier)
            .IsUnique();
    }
}