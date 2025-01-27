using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Multitenancy.Entities;

public class TenantEntity
{
    [Key, Column("Id")]
    public Guid Id { get; set; }
    public string Identifier { get; set; } // the identifier can change but not the Id
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public TenantEntity()
    {
        Identifier = string.Empty;
    }

    public TenantEntity(string identifier)
    {
        Identifier = identifier;
    }
}
