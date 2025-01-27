namespace Multitenancy.Exceptions;

public class TenantOperationException : TenantException
{
    public TenantOperationException(string message, Exception? innerException = null)
        : base($"Failed to perform tenant operation: {message}", innerException)
    { }
}