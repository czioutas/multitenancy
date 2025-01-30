using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Multitenancy.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant system. Each tenant is a distinct
/// organizational unit with its own isolated set of data and resources.
/// </summary>
/// <remarks>
/// TenantEntity serves as the root entity for tenant isolation, where each tenant
/// has its own hierarchy of related entities. The entity tracks creation and update
/// timestamps automatically through the database context.
/// </remarks>
public class TenantEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    /// <remarks>
    /// This is the primary key that remains constant throughout the tenant's lifecycle,
    /// even if the tenant's identifier changes.
    /// </remarks>
    [Key, Column("Id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable identifier for the tenant.
    /// </summary>
    /// <remarks>
    /// Unlike the Id, this identifier can be changed over time. It should be
    /// unique across all tenants and is typically used as a friendly name or
    /// reference for the tenant.
    /// </remarks>
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the tenant was created.
    /// </summary>
    /// <remarks>
    /// This value is automatically set when the tenant is first created and
    /// cannot be modified afterwards.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the tenant was last updated.
    /// </summary>
    /// <remarks>
    /// This value is automatically updated whenever the tenant entity is modified.
    /// A null value indicates that the tenant has never been updated since creation.
    /// </remarks>
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool Deleted { get; set; } = false;

    /// <summary>
    /// Gets or sets a flag indicating whether the tenant has been deleted.
    /// </summary>
    /// <remarks>
    /// When true, this flag implements a soft-delete pattern, hiding the tenant
    /// from normal queries while preserving the data. Soft-deleted tenants are
    /// filtered out by default in queries.
    /// </remarks>
    public bool Deleted { get; set; }

    /// <summary>
    /// Initializes a new instance of the TenantEntity class with an empty identifier.
    /// </summary>
    public TenantEntity()
    {
        Identifier = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the TenantEntity class with the specified identifier.
    /// </summary>
    /// <param name="identifier">The human-readable identifier for the tenant.</param>
    /// <remarks>
    /// Use this constructor when creating a new tenant with a known identifier.
    /// The CreatedAt timestamp will be automatically set when the entity is saved.
    /// </remarks>
    public TenantEntity(string identifier)
    {
        Identifier = identifier;
    }
}