namespace Multitenancy.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a tenant with an identifier that already exists.
/// </summary>
/// <remarks>
/// This exception helps maintain the uniqueness constraint of tenant identifiers
/// by preventing duplicate tenant creation.
/// </remarks>
public class TenantAlreadyExistsException : TenantException
{
    /// <summary>
    /// Gets the identifier that caused the duplicate tenant conflict.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Initializes a new instance of the TenantAlreadyExistsException class.
    /// </summary>
    /// <param name="identifier">The identifier that already exists in the system.</param>
    /// <remarks>
    /// The exception message is automatically formatted to include the conflicting identifier.
    /// </remarks>
    public TenantAlreadyExistsException(string identifier)
        : base($"Tenant with Identifier '{identifier}' already exists")
    {
        Identifier = identifier;
    }
}
