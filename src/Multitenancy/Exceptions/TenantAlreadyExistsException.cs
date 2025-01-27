namespace Multitenancy.Exceptions;

public class TenantAlreadyExistsException : TenantException
{
    public string Identifier { get; }

    public TenantAlreadyExistsException(string identifier)
        : base($"Tenant with Identifier '{identifier}' already exists")
    {
        Identifier = identifier;
    }
}