using Microsoft.AspNetCore.Http;
using Multitenancy.Services;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Middleware;

public class MultiTenantServiceMiddleware : IMiddleware
{
    private readonly IRequestTenant _requestTenant;
    private readonly ITenantConfiguration _config;
    private readonly ILogger<MultiTenantServiceMiddleware> _logger;

    public MultiTenantServiceMiddleware(IRequestTenant requestTenant, ITenantConfiguration config, ILogger<MultiTenantServiceMiddleware> logger)

    {
        _config = config;
        _requestTenant = requestTenant;
        _logger = logger;

    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogInformation("MultiTenantServiceMiddleware is executing.");

        Guid? tenantId = null;

        if (_config.GetCurrentUserTenantId != null)
        {
            tenantId = _config.GetCurrentUserTenantId();
        }

        if (tenantId is null || !tenantId.HasValue || tenantId == Guid.Empty)
        {
            _logger.LogInformation("No value from provided function - checking for header");
            tenantId = FallbackExtractTenantIdFromRequest(context);
        }

        if (tenantId != null && tenantId.HasValue && tenantId != Guid.Empty)
        {
            _requestTenant.SetTenantId(tenantId.Value);
        }

        // _config.SetTenantId(tenantId ?? Guid.Empty);

        _logger.LogInformation("TENANT " + tenantId + " _");

        await next(context);
    }

    private Guid FallbackExtractTenantIdFromRequest(HttpContext context)
    {
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return Guid.TryParse(tenantIdHeader, out var tenantId) ? tenantId : Guid.Empty;
    }
}
