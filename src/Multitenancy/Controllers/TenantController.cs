using Multitenancy.Exceptions;
using Multitenancy.Models;
using Multitenancy.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Controllers;

/// <summary>
/// Controller for managing multi-tenant operations and configurations.
/// Provides endpoints for retrieving, updating, and deleting tenant information.
/// </summary>
/// <remarks>
/// All endpoints in this controller require authentication.
/// Operations are restricted to the tenant associated with the authenticated user.
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;
    private readonly ITenantConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the TenantController.
    /// </summary>
    /// <param name="tenantService">Service for managing tenant operations.</param>
    /// <param name="config">Configuration service for tenant settings.</param>
    /// <param name="logger">Logger for capturing diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
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

    /// <summary>
    /// Creates a new tenant with the specified identifier.
    /// </summary>
    /// <remarks>
    /// This endpoint creates a new tenant with a given string identifier.
    /// The identifier must be unique across all tenants in the system.
    /// </remarks>
    /// <param name="identifier">The unique identifier for the new tenant.</param>
    /// <returns>The newly created tenant information.</returns>
    /// <response code="201">Returns the newly created tenant.</response>
    /// <response code="400">If the identifier is invalid or already exists.</response>
    /// <response code="500">If there's an unexpected server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Create([FromBody] string identifier)
    {
        var userId = _config.GetCurrentUserId();

        _logger.LogDebug("Create tenant request received. UserId: {UserId}, Identifier: {Identifier}",
            userId, identifier);

        try
        {
            _logger.LogInformation("Creating new tenant with identifier: {Identifier}", identifier);
            var result = await _tenantService.CreateAsync(identifier);

            _logger.LogDebug("Successfully created tenant. Id: {TenantId}, Identifier: {Identifier}",
                result.Id, result.Identifier);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (TenantAlreadyExistsException ex)
        {
            _logger.LogError(ex, "Tenant already exists with identifier: {Identifier}", identifier);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant Identifier Invalid.",
                Detail = ex.Message
            });
        }
        catch (TenantException ex)
        {
            _logger.LogError(ex, "Tenant error occurred during creation. Identifier: {Identifier}, Error: {ErrorMessage}",
                identifier, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant Error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating tenant with identifier: {Identifier}",
                identifier);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred while creating the tenant"
            });
        }
    }

    /// <summary>
    /// Retrieves the tenant information for the current authenticated user.
    /// </summary>
    /// <remarks>
    /// This endpoint returns detailed information about the tenant associated with the authenticated user.
    /// The tenant identification is handled automatically through the authentication context.
    /// </remarks>
    /// <returns>The tenant information if found.</returns>
    /// <response code="200">Returns the tenant information successfully.</response>
    /// <response code="400">If there's a validation error or invalid tenant operation.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the tenant is not found.</response>
    /// <response code="500">If there's an unexpected server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Get()
    {
        _logger.LogDebug("Get tenant request received for TenantId");
        var tenantId = _config.GetCurrentUserTenantId();

        try
        {
            _logger.LogInformation("Retrieving tenant information for Tenant");
            var result = await _tenantService.GetAsync(tenantId);

            _logger.LogDebug("Successfully retrieved tenant information for Tenant");
            return Ok(result);
        }
        catch (TenantNotFoundException ex)
        {
            _logger.LogError(ex, "Tenant info not found");
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Not Found",
                Detail = ex.Message
            });
        }
        catch (TenantException ex)
        {
            _logger.LogError(ex, "Tenant error occurred");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant Error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving tenant");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Updates the identifier of a specific tenant.
    /// </summary>
    /// <remarks>
    /// This endpoint allows updating the identifier of the tenant associated with the authenticated user.
    /// The operation will only succeed if:
    /// - The specified tenant ID matches the authenticated user's tenant
    /// - The new identifier is not already in use by another tenant
    /// - The tenant exists and is active
    /// </remarks>
    /// <param name="id">The unique identifier of the tenant to update.</param>
    /// <param name="newIdentifier">The new identifier to assign to the tenant.</param>
    /// <returns>The updated tenant information.</returns>
    /// <response code="200">Returns the updated tenant information.</response>
    /// <response code="400">If the new identifier is invalid or already exists.</response>
    /// <response code="404">If the tenant is not found or the user doesn't have access to it.</response>
    /// <response code="500">If there's an unexpected server error.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantModel>> Update(Guid id, [FromBody] string newIdentifier)
    {
        var tenantId = _config.GetCurrentUserTenantId();
        var userId = _config.GetCurrentUserId();

        _logger.LogDebug("Update tenant request received. TenantId: {TenantId}, UserId: {UserId}, NewIdentifier: {NewIdentifier}",
            tenantId, userId, newIdentifier);

        if (id != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to update tenant. RequestedTenantId: {RequestedTenantId}, UserTenantId: {UserTenantId}",
                id, tenantId);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Not Found",
                Detail = "Tenant with Id '{id}' was not found."
            });
        }

        try
        {
            _logger.LogInformation("Updating tenant {TenantId} with new identifier: {NewIdentifier}", id, newIdentifier);
            var result = await _tenantService.UpdateAsync(id, newIdentifier);

            _logger.LogDebug("Successfully updated tenant {TenantId}", id);
            return Ok(result);
        }
        catch (TenantNotFoundException ex)
        {
            _logger.LogError(ex, "Tenant not found for update. TenantId: {TenantId}", id);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Not Found",
                Detail = ex.Message
            });
        }
        catch (TenantAlreadyExistsException ex)
        {
            _logger.LogError(ex, "New identifier already exists. TenantId: {TenantId}, NewIdentifier: {NewIdentifier}",
                id, newIdentifier);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Identifier Already Exists",
                Detail = ex.Message
            });
        }
        catch (TenantException ex)
        {
            _logger.LogError(ex, "Tenant error occurred during update. TenantId: {TenantId}, Error: {ErrorMessage}",
                id, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant Error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating tenant. TenantId: {TenantId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred"
            });
        }
    }
}