namespace Multitenancy.Exceptions;

/// <summary>
/// Base exception class for all tenant-related exceptions in the system.
/// Provides a foundation for more specific tenant exception types.
/// </summary>
/// <remarks>
/// This class serves as the parent for all tenant-specific exceptions,
/// allowing for consistent exception handling and logging patterns.
/// </remarks>
public class TenantException : Exception
{
    /// <summary>
    /// Initializes a new instance of the TenantException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the TenantException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TenantException(string message, Exception? innerException) : base(message, innerException) { }
}