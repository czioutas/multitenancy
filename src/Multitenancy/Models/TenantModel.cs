using System.ComponentModel.DataAnnotations;

namespace Multitenancy.Models;

public sealed record TenantModel
{
    [Required]
    public required Guid Id { get; set; }
    [Required]
    public required string Identifier { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool Deleted { get; set; } = false;
}
