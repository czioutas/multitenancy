using Multitenancy.Entities;
using Multitenancy.Exceptions;
using Multitenancy.Models;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Services;

/// <summary>
/// Provides core functionality for managing tenants in a multi-tenant application.
/// </summary>
/// <remarks>
/// This service handles all tenant-related operations including creation, retrieval,
/// updates, and deletion. It implements soft delete functionality where tenants are
/// marked as deleted rather than being physically removed from the database.
/// </remarks>
public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant with the specified identifier.
    /// </summary>
    /// <param name="tenantIdentifier">A unique identifier for the tenant.</param>
    /// <returns>A <see cref="TenantModel"/> representing the newly created tenant.</returns>
    /// <exception cref="TenantAlreadyExistsException">Thrown when a tenant with the specified identifier already exists.</exception>
    /// <exception cref="TenantOperationException">Thrown when the tenant creation fails due to a database or system error.</exception>
    /// <example>
    /// <code>
    /// var tenant = await tenantService.CreateAsync("new-company");
    /// Console.WriteLine($"Created tenant with ID: {tenant.Id}");
    /// </code>
    /// </example>
    Task<TenantModel> CreateAsync(string tenantIdentifier);

    /// <summary>
    /// Finds a tenant by their unique identifier.
    /// </summary>
    /// <param name="tenantIdentifier">The unique identifier of the tenant.</param>
    /// <returns>A <see cref="TenantModel"/> containing the tenant's information.</returns>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant is found with the specified identifier.</exception>
    /// <exception cref="TenantOperationException">Thrown when the operation fails due to a database or system error.</exception>
    Task<TenantModel> FindByIdentifierAsync(string tenantIdentifier);

    /// <summary>
    /// Retrieves a tenant by their unique GUID.
    /// </summary>
    /// <param name="tenantId">The unique GUID of the tenant.</param>
    /// <returns>A <see cref="TenantModel"/> containing the tenant's information.</returns>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant is found with the specified ID.</exception>
    /// <exception cref="TenantOperationException">Thrown when the operation fails due to a database or system error.</exception>
    Task<TenantModel> GetAsync(Guid tenantId);

    /// <summary>
    /// Retrieves a tenant by their identifier string.
    /// </summary>
    /// <param name="tenantIdentifier">The unique identifier of the tenant.</param>
    /// <returns>A <see cref="TenantModel"/> containing the tenant's information.</returns>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant is found with the specified identifier.</exception>
    /// <exception cref="TenantOperationException">Thrown when the operation fails due to a database or system error.</exception>
    Task<TenantModel> GetAsync(string tenantIdentifier);

    /// <summary>
    /// Generates a random, unique tenant identifier that can be used for tenant creation.
    /// </summary>
    /// <returns>A string containing a randomly generated tenant identifier.</returns>
    /// <remarks>
    /// This method generates an identifier in the format: "City-Street-FirstName-JobArea".
    /// The generated identifier is not guaranteed to be unique in the database,
    /// so it should be validated before use in tenant creation.
    /// </remarks>
    string GetRandomIdentifier();

    /// <summary>
    /// Updates a tenant's identifier to a new value.
    /// </summary>
    /// <param name="tenantId">The unique GUID of the tenant to update.</param>
    /// <param name="newIdentifier">The new identifier to assign to the tenant.</param>
    /// <returns>A <see cref="TenantModel"/> containing the updated tenant information.</returns>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant is found with the specified ID.</exception>
    /// <exception cref="TenantAlreadyExistsException">Thrown when another tenant already uses the new identifier.</exception>
    /// <exception cref="TenantOperationException">Thrown when the operation fails due to a database or system error.</exception>
    /// <example>
    /// <code>
    /// try 
    /// {
    ///     var updatedTenant = await tenantService.UpdateAsync(
    ///         Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), 
    ///         "new-company-name"
    ///     );
    ///     Console.WriteLine($"Updated tenant identifier to: {updatedTenant.Identifier}");
    /// }
    /// catch (TenantNotFoundException ex)
    /// {
    ///     Console.WriteLine("Tenant not found");
    /// }
    /// catch (TenantAlreadyExistsException ex)
    /// {
    ///     Console.WriteLine("New identifier is already in use");
    /// }
    /// </code>
    /// </example>
    Task<TenantModel> UpdateAsync(Guid tenantId, string newIdentifier);

    /// <summary>
    /// Performs a soft delete of the specified tenant.
    /// </summary>
    /// <param name="tenantId">The unique GUID of the tenant to delete.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    /// <remarks>
    /// This method implements soft delete functionality. The tenant is not physically
    /// removed from the database but is marked as deleted. Once deleted, the tenant:
    /// - Will not appear in query results
    /// - Cannot be updated
    /// - Cannot be deleted again
    /// - Cannot have its identifier reused for new tenants
    /// </remarks>
    /// <exception cref="TenantNotFoundException">Thrown when no tenant is found with the specified ID.</exception>
    /// <exception cref="TenantOperationException">Thrown when the operation fails due to a database or system error.</exception>
    /// <example>
    /// <code>
    /// try 
    /// {
    ///     bool deleted = await tenantService.DeleteAsync(
    ///         Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
    ///     );
    ///     if (deleted)
    ///     {
    ///         Console.WriteLine("Tenant was successfully deleted");
    ///     }
    /// }
    /// catch (TenantNotFoundException ex)
    /// {
    ///     Console.WriteLine("Tenant not found");
    /// }
    /// </code>
    /// </example>
    Task<bool> DeleteAsync(Guid tenantId);
}

/// <summary>
/// Implementation of the ITenantService interface that provides tenant management functionality.
/// </summary>
/// <remarks>
/// This service is designed to be used with Entity Framework Core and requires:
/// - A DbContext that includes TenantEntity
/// - The IRequestTenant service for tracking the current tenant context
/// - Proper configuration through TenantConfiguration
/// 
/// Usage example:
/// <code>
/// services.AddMultiTenancy&lt;YourDbContext&gt;(options =>
/// {
///     options
///         .WithDbContext&lt;YourDbContext&gt;()
///         .WithUser&lt;YourUser&gt;()
///         .WithRole&lt;YourRole&gt;()
///         .WithCurrentUserProvider(sp => GetUserId())
///         .WithCurrentUserTenantProvider(sp => GetTenantId());
/// });
/// </code>
/// </remarks>
public class TenantService : ITenantService
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TenantService> _logger;
    private readonly ITenantConfiguration _config;

    private DbSet<TenantEntity> Tenants => _dbContext.Set<TenantEntity>();

    public TenantService(
        IServiceProvider serviceProvider,
        ITenantConfiguration config,
        ILogger<TenantService> logger
    )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dbContext = (DbContext)(serviceProvider.GetService(_config.DbContextType)
                    ?? throw new InvalidOperationException($"Unable to resolve a service for type '{_config.DbContextType}'."));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("TenantService initialized with DbContext type: {DbContextType}", _config.DbContextType.Name);
    }

    public async Task<TenantModel> CreateAsync(string tenantIdentifier)
    {
        try
        {
            _logger.LogInformation("Attempting to create new tenant with identifier: {TenantIdentifier}", tenantIdentifier);

            var tenantEntity = await Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);

            if (tenantEntity is not null)
            {
                _logger.LogWarning("Tenant creation failed - identifier already exists: {TenantIdentifier}", tenantIdentifier);
                throw new TenantAlreadyExistsException(tenantIdentifier);
            }

            var newTenantEntity = new TenantEntity() { Identifier = tenantIdentifier };
            await Tenants.AddAsync(newTenantEntity);
            var result = await _dbContext.SaveChangesAsync();

            if (result != 1)
            {
                _logger.LogError("Failed to create tenant. Expected 1 row affected, but got {AffectedRows}", result);
                throw new TenantOperationException("Failed to create tenant");
            }
            else
            {
                _logger.LogInformation("Successfully created tenant. ID: {TenantId}, Identifier: {TenantIdentifier}", newTenantEntity.Id, newTenantEntity.Identifier);
                return new TenantModel()
                {
                    Id = newTenantEntity.Id,
                    Identifier = newTenantEntity.Identifier,
                    CreatedAt = newTenantEntity.CreatedAt,
                    UpdatedAt = newTenantEntity.UpdatedAt
                };
            }
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            _logger.LogError(ex, "Unexpected error while creating tenant with identifier: {TenantIdentifier}", tenantIdentifier);
            throw new TenantOperationException($"Failed to create tenant '{tenantIdentifier}'", ex);
        }
    }

    public async Task<TenantModel> FindByIdentifierAsync(string tenantIdentifier)
    {
        try
        {
            _logger.LogDebug("Searching for tenant by identifier: {TenantIdentifier}", tenantIdentifier);

            var tenantEntity = await Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier && !t.Deleted);

            if (tenantEntity is null)
            {
                _logger.LogInformation("No tenant found with identifier: {TenantIdentifier}", tenantIdentifier);
                throw new TenantNotFoundException(tenantIdentifier);
            }

            _logger.LogDebug("Found tenant. ID: {TenantId}, Identifier: {TenantIdentifier}", tenantEntity.Id, tenantEntity.Identifier);

            return new TenantModel()
            {
                Id = tenantEntity.Id,
                Identifier = tenantEntity.Identifier,
                CreatedAt = tenantEntity.CreatedAt,
                UpdatedAt = tenantEntity.UpdatedAt
            };
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            _logger.LogError(ex, "Error occurred while finding tenant with identifier: {TenantIdentifier}", tenantIdentifier);
            throw new TenantOperationException($"Failed to find tenant with identifier '{tenantIdentifier}'", ex);
        }
    }

    public async Task<TenantModel> GetAsync(Guid tenantId)
    {
        try
        {
            _logger.LogDebug("Retrieving tenant by ID: {TenantId}", tenantId);

            TenantEntity? tenantEntity = await Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.Deleted);

            if (tenantEntity is null)
            {
                _logger.LogInformation("No tenant found with ID: {TenantId}", tenantId);
                throw new TenantNotFoundException(tenantId);
            }

            _logger.LogDebug("Successfully retrieved tenant. ID: {TenantId}, Identifier: {TenantIdentifier}", tenantEntity.Id, tenantEntity.Identifier);

            return new TenantModel()
            {
                Id = tenantEntity.Id,
                Identifier = tenantEntity.Identifier,
                CreatedAt = tenantEntity.CreatedAt,
                UpdatedAt = tenantEntity.UpdatedAt
            };
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            _logger.LogError(ex, "Error occurred while retrieving tenant with ID: {TenantId}", tenantId);
            throw new TenantOperationException($"Failed to get tenant with Id '{tenantId}'", ex);
        }
    }

    public async Task<TenantModel> GetAsync(string tenantIdentifier)
    {
        try
        {
            TenantEntity? tenantEntity = await Tenants
            .FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier && !t.Deleted);

            if (tenantEntity is null)
            {
                throw new TenantNotFoundException(tenantIdentifier);
            }

            return new TenantModel()
            {
                Id = tenantEntity.Id,
                Identifier = tenantEntity.Identifier,
                CreatedAt = tenantEntity.CreatedAt,
                UpdatedAt = tenantEntity.UpdatedAt
            };
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            throw new TenantOperationException($"Failed to get tenant with Identifier '{tenantIdentifier}'", ex);
        }
    }

    public async Task<TenantModel> UpdateAsync(Guid tenantId, string newIdentifier)
    {
        try
        {
            _logger.LogInformation("Attempting to update tenant {TenantId} with new identifier: {NewIdentifier}", tenantId, newIdentifier);

            var existingTenant = await Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.Deleted);

            if (existingTenant == null)
            {
                _logger.LogWarning("Update failed - tenant not found. ID: {TenantId}", tenantId);
                throw new TenantNotFoundException(tenantId);
            }

            // Check if new identifier is already in use by another tenant
            var identifierExists = await Tenants.AnyAsync(t => t.Identifier == newIdentifier && t.Id != tenantId && !t.Deleted);

            if (identifierExists)
            {
                _logger.LogWarning("Update failed - new identifier already exists: {NewIdentifier}", newIdentifier);
                throw new TenantAlreadyExistsException(newIdentifier);
            }

            // Update tenant
            existingTenant.Identifier = newIdentifier;

            var result = await _dbContext.SaveChangesAsync();

            if (result != 1)
            {
                _logger.LogError("Failed to update tenant. Expected 1 row affected, but got {AffectedRows}", result);
                throw new TenantOperationException($"Failed to update tenant '{tenantId}'");
            }

            _logger.LogInformation("Successfully updated tenant {TenantId}. New identifier: {NewIdentifier}", tenantId, newIdentifier);
            return new TenantModel()
            {
                Id = existingTenant.Id,
                Identifier = existingTenant.Identifier,
                CreatedAt = existingTenant.CreatedAt,
                UpdatedAt = existingTenant.UpdatedAt,
                Deleted = existingTenant.Deleted
            };
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            _logger.LogError(ex, "Unexpected error while updating tenant {TenantId} to {NewIdentifier}", tenantId, newIdentifier);
            throw new TenantOperationException($"Failed to update tenant '{tenantId}'", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Attempting to delete tenant: {TenantId}", tenantId);
            var tenant = await Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.Deleted);

            if (tenant == null)
            {
                _logger.LogWarning("Delete failed - tenant not found. ID: {TenantId}", tenantId);
                throw new TenantNotFoundException(tenantId);
            }

            // Soft delete
            tenant.Deleted = true;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await _dbContext.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("Successfully deleted tenant. ID: {TenantId}, Identifier: {TenantIdentifier}", tenant.Id, tenant.Identifier);
            }
            else
            {
                _logger.LogError("Failed to delete tenant. Expected at least 1 row affected, but got {AffectedRows}", result);
            }

            return result > 0;
        }
        catch (Exception ex) when (ex is not TenantException)
        {
            _logger.LogError(ex, "Unexpected error while deleting tenant: {TenantId}", tenantId);
            throw new TenantOperationException($"Failed to delete tenant '{tenantId}'", ex);
        }
    }

    public string GetRandomIdentifier()
    {
        _logger.LogDebug("Generating random tenant identifier");
        Faker a = new();
        string newRandomIdentifier = a.Address.City() + "-" + a.Address.StreetName() + "-" + a.Name.FirstName() + "-" + a.Name.JobArea().Trim();
        newRandomIdentifier = newRandomIdentifier.Replace(" ", "");
        _logger.LogDebug("Generated random identifier: {RandomIdentifier}", newRandomIdentifier);
        return newRandomIdentifier;
    }
}
