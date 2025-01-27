namespace Multitenancy.Exceptions;

public class TenantException : Exception
{
    public TenantException(string message) : base(message) { }
    public TenantException(string message, Exception? innerException) : base(message, innerException) { }
}

public class TenantNotFoundException : TenantException
{
    public string? Identifier { get; }
    public Guid? Id { get; }
    public Guid UserId { get; }

    public TenantNotFoundException(string identifier)
        : base($"Tenant with identifier '{identifier}' was not found.")
    {
        Identifier = identifier;
    }

    public TenantNotFoundException(Guid id)
        : base($"Tenant with id '{id}' was not found.")
    {
        Id = id;
    }

    public TenantNotFoundException(Guid id, Guid userId)
    : base($"Tenant '{id}' not found for user '{userId}'")
    {
        Id = id;
        UserId = userId;
    }
}