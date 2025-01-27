using Multitenancy.Exceptions;
using Multitenancy.Models;
using Multitenancy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;
    private readonly ITenantConfiguration _config;

    public TenantController(
        ITenantService tenantService,
        ITenantConfiguration config,
        ILogger<TenantController> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("TenantController initialized with services: TenantService, TenantConfiguration");
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Get()
    {
        var tenantId = _config.GetCurrentUserTenantId();
        var userId = _config.GetCurrentUserId();

        _logger.LogDebug("Get tenant request received for TenantId: {TenantId}, UserId: {UserId}", tenantId, userId);

        try
        {
            _logger.LogInformation("Retrieving tenant information for TenantId: {TenantId}", tenantId);
            var result = await _tenantService.GetAsync(tenantId: tenantId);

            _logger.LogDebug("Successfully retrieved tenant information for TenantId: {TenantId}", tenantId);
            return Ok(result);
        }
        catch (TenantNotFoundException ex)
        {
            _logger.LogError(ex, "Tenant not found for TenantId: {TenantId}", tenantId);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Not Found",
                Detail = ex.Message
            });
        }
        catch (TenantException ex)
        {
            _logger.LogError(ex, "Tenant error occurred for TenantId: {TenantId}. Error: {ErrorMessage}",
                tenantId, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant Error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving tenant for TenantId: {TenantId}",
                tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred"
            });
        }
    }
}