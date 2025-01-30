using Microsoft.AspNetCore.Http;
using Multitenancy.Services;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Middleware;

/// <summary>
/// Middleware component that handles tenant context resolution and setup for each HTTP request.
/// Implements ASP.NET Core's middleware pattern to intercept requests and establish tenant context.
/// </summary>
/// <remarks>
/// This middleware is responsible for:
/// - Extracting tenant information from the current request
/// - Setting up the tenant context for the request pipeline
/// - Falling back to header-based tenant identification when needed
/// - Logging tenant resolution details for debugging
/// </remarks>
public class MultiTenantServiceMiddleware : IMiddleware
{
    /// <summary>
    /// Service for managing tenant context within the current request scope.
    /// </summary>
    private readonly IRequestTenant _requestTenant;

    /// <summary>
    /// Configuration service providing tenant-related settings and identification strategies.
    /// </summary>
    private readonly ITenantConfiguration _config;

    /// <summary>
    /// Logger instance for diagnostic and debugging information.
    /// </summary>
    private readonly ILogger<MultiTenantServiceMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the MultiTenantServiceMiddleware class.
    /// </summary>
    /// <param name="requestTenant">Service for managing tenant context.</param>
    /// <param name="config">Service providing tenant configuration.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
    public MultiTenantServiceMiddleware(
        IRequestTenant requestTenant,
        ITenantConfiguration config,
        ILogger<MultiTenantServiceMiddleware> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an HTTP request to establish tenant context.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <returns>A Task representing the completion of request processing.</returns>
    /// <remarks>
    /// The method follows this process:
    /// 1. Attempts to get tenant ID from the configured provider
    /// 2. Falls back to header-based identification if necessary
    /// 3. Sets the tenant context if a valid tenant ID is found
    /// 4. Logs the resolution process for debugging
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogInformation("MultiTenantServiceMiddleware is executing.");

        Guid? tenantId = null;

        // Try to get tenant ID from configured provider
        if (_config.GetCurrentUserTenantId != null)
        {
            tenantId = _config.GetCurrentUserTenantId();
        }

        // Fall back to header-based identification if needed
        if (tenantId is null || !tenantId.HasValue || tenantId == Guid.Empty)
        {
            _logger.LogInformation("No value from provided function - checking for header");
            tenantId = FallbackExtractTenantIdFromRequest(context);
        }

        // Set tenant context if valid ID found
        if (tenantId != null && tenantId.HasValue && tenantId != Guid.Empty)
        {
            _requestTenant.SetTenantId(tenantId.Value);
        }

        _logger.LogInformation("TENANT " + tenantId + " _");

        await next(context);
    }

    /// <summary>
    /// Extracts tenant ID from the request headers as a fallback identification method.
    /// </summary>
    /// <param name="context">The HTTP context containing the request headers.</param>
    /// <returns>The extracted tenant ID, or Guid.Empty if not found or invalid.</returns>
    /// <remarks>
    /// This method looks for the 'X-Tenant-Id' header and attempts to parse it as a GUID.
    /// It's used as a fallback when the primary tenant identification strategy fails.
    /// </remarks>
    private Guid FallbackExtractTenantIdFromRequest(HttpContext context)
    {
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(tenantIdHeader, out var tenantId) ? tenantId : Guid.Empty;
    }
}