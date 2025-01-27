using Multitenancy.Entities;
using Multitenancy.Exceptions;
using Multitenancy.Models;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Multitenancy.Services;

public interface ITenantService
{
    Task<TenantModel> CreateAsync(string tenantIdentifier);
    Task<TenantModel> FindByIdentifierAsync(string tenantIdentifier);
    Task<TenantModel> GetAsync(Guid tenantId);
    Task<TenantModel> GetAsync(string tenantIdentifier);
    string GetRandomIdentifier();
}

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
    }

    public async Task<TenantModel> CreateAsync(string tenantIdentifier)
    {
        try
        {
            var tenantEntity = await Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);

            if (tenantEntity is not null)
            {
                throw new TenantAlreadyExistsException(tenantIdentifier);
            }

            var newTenantEntity = new TenantEntity() { Identifier = tenantIdentifier };
            await Tenants.AddAsync(newTenantEntity);
            var result = await _dbContext.SaveChangesAsync();

            if (result != 1)
            {
                throw new TenantOperationException("Failed to create tenant");
            }
            else
            {
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
            throw new TenantOperationException($"Failed to create tenant '{tenantIdentifier}'", ex);

            throw;
        }
    }

    public async Task<TenantModel> FindByIdentifierAsync(string tenantIdentifier)
    {
        try
        {
            var tenantEntity = await Tenants.FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);

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
            throw new TenantOperationException($"Failed to find tenant with identifier '{tenantIdentifier}'", ex);
        }
    }

    public async Task<TenantModel> GetAsync(Guid tenantId)
    {
        try
        {
            TenantEntity? tenantEntity = await Tenants
            .Where(t => t.Id == tenantId)
            .FirstOrDefaultAsync();

            if (tenantEntity is null)
            {
                throw new TenantNotFoundException(tenantId);
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
            throw new TenantOperationException($"Failed to get tenant with Id '{tenantId}'", ex);
        }
    }

    public async Task<TenantModel> GetAsync(string tenantIdentifier)
    {
        try
        {
            TenantEntity? tenantEntity = await Tenants
            .Where(t => t.Identifier == tenantIdentifier)
            .FirstOrDefaultAsync();

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

    public string GetRandomIdentifier()
    {
        Faker a = new();
        string newRandomIdentifier = a.Address.City() + "-" + a.Address.StreetName() + "-" + a.Name.FirstName() + "-" + a.Name.JobArea().Trim();
        newRandomIdentifier = newRandomIdentifier.Replace(" ", "");
        return newRandomIdentifier;
    }
}
