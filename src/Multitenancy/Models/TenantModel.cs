using System.ComponentModel.DataAnnotations;

namespace Multitenancy.Models;

/// <summary>
/// Represents a tenant entity in a multi-tenant system.
/// This record contains the core information needed to identify and manage a tenant.
/// </summary>
public sealed record TenantModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    [Required]
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique string identifier for the tenant.
    /// This is typically used as a human-readable identifier for the tenant.
    /// </summary>
    [Required]
    public required string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the tenant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the tenant was last updated.
    /// Null if the tenant has never been updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant has been marked as deleted.
    /// Default value is false.
    /// </summary>
    public bool Deleted { get; set; } = false;
}
