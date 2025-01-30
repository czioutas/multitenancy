namespace Multitenancy.Exceptions;

/// <summary>
/// Exception thrown when a tenant-related operation fails due to an unexpected error.
/// This serves as a general-purpose exception for tenant operations that don't fit
/// more specific exception types.
/// </summary>
/// <remarks>
/// This exception typically wraps lower-level exceptions that occur during tenant
/// operations, providing additional context about the failed operation.
/// </remarks>
public class TenantOperationException : TenantException
{
    /// <summary>
    /// Initializes a new instance of the TenantOperationException class.
    /// </summary>
    /// <param name="message">A message describing the failed operation.</param>
    /// <param name="innerException">The underlying exception that caused the operation to fail, if any.</param>
    /// <remarks>
    /// The message is automatically prefixed with "Failed to perform tenant operation: "
    /// to provide consistent error messaging.
    /// </remarks>
    public TenantOperationException(string message, Exception? innerException = null)
        : base($"Failed to perform tenant operation: {message}", innerException)
    { }
}