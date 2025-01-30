namespace Multitenancy.Exceptions;

/// <summary>
/// Exception thrown when a tenant cannot be found in the system.
/// This can occur when searching by ID, identifier, or user association.
/// </summary>
/// <remarks>
/// This exception includes different constructors to handle various tenant lookup scenarios,
/// preserving the context of the failed lookup attempt.
/// </remarks>
public class TenantNotFoundException : TenantException
{
    /// <summary>
    /// Gets the identifier used in the tenant lookup attempt, if applicable.
    /// </summary>
    public string? Identifier { get; }

    /// <summary>
    /// Gets the ID used in the tenant lookup attempt, if applicable.
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Gets the user ID associated with the tenant lookup attempt, if applicable.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Initializes a new instance of the TenantNotFoundException class for lookups by identifier.
    /// </summary>
    /// <param name="identifier">The identifier used in the failed lookup attempt.</param>
    public TenantNotFoundException(string identifier)
        : base($"Tenant with identifier '{identifier}' was not found.")
    {
        Identifier = identifier;
    }

    /// <summary>
    /// Initializes a new instance of the TenantNotFoundException class for lookups by ID.
    /// </summary>
    /// <param name="id">The ID used in the failed lookup attempt.</param>
    public TenantNotFoundException(Guid id)
        : base($"Tenant with id '{id}' was not found.")
    {
        Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the TenantNotFoundException class for lookups by ID and user association.
    /// </summary>
    /// <param name="id">The tenant ID used in the failed lookup attempt.</param>
    /// <param name="userId">The user ID associated with the lookup attempt.</param>
    public TenantNotFoundException(Guid id, Guid userId)
        : base($"Tenant '{id}' not found for user '{userId}'")
    {
        Id = id;
        UserId = userId;
    }
}