using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Multitenancy;

public interface ITenantConfiguration
{
    Type DbContextType { get; }
    Type UserType { get; }
    Type RoleType { get; }
    Func<object> GetCurrentUserId { get; }
    Func<Guid> GetCurrentUserTenantId { get; }
    bool WithTenantEntity { get; }
    void SetTenantId(Guid tenantId);
}

public class TenantConfiguration : ITenantConfiguration
{
    private Type? _dbContextType;
    private Type? _userType;
    private Type? _roleType;
    private Func<object>? _getCurrentUserId;
    private Func<Guid>? _getCurrentUserTenantId;
    private Guid? _tenantId;
    private bool _withTenantEntity = true;

    public Type DbContextType
    {
        get
        {
            if (_dbContextType == null)
            {
                throw new InvalidOperationException("DbContextType has not been set.");
            }
            return _dbContextType;
        }
        set
        {
            _dbContextType = value;
        }
    }

    public Type? UserType
    {
        get
        {
            return _userType;
        }
        set
        {
            _userType = value;
        }
    }

    public Type? RoleType
    {
        get
        {
            return _roleType;
        }
        set
        {
            _roleType = value;
        }
    }

    public Func<object> GetCurrentUserId
    {
        get
        {
            if (_getCurrentUserId == null)
            {
                throw new InvalidOperationException("GetCurrentUserId has not been set.");
            }
            return _getCurrentUserId;
        }
        set
        {
            _getCurrentUserId = value;
        }
    }

    public Func<Guid> GetCurrentUserTenantId
    {
        get
        {
            if (_getCurrentUserTenantId == null)
            {
                return null;
            }
            return _getCurrentUserTenantId;
        }
        set
        {
            _getCurrentUserTenantId = value;
        }
    }

    public bool WithTenantEntity
    {
        get
        {
            return _withTenantEntity;
        }
        set
        {
            _withTenantEntity = value;
        }
    }

    public void SetTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        GetCurrentUserTenantId = () => _tenantId ?? Guid.Empty;
    }

    // Method to check for null and throw an exception
    private void EnsureRequiredPropertiesSet()
    {
        if (DbContextType == null)
            throw new InvalidOperationException("DbContextType must be set.");
        if (UserType == null && DbContextType == typeof(TenantIdentityDbContext))
            throw new InvalidOperationException("UserType must be set for TenantIdentityDbContext.");
        if (RoleType == null && DbContextType == typeof(TenantIdentityDbContext))
            throw new InvalidOperationException("RoleType must be set.");
        if (GetCurrentUserId == null)
            throw new InvalidOperationException("GetCurrentUserId must be set.");
        if (GetCurrentUserTenantId == null)
            throw new InvalidOperationException("GetCurrentUserTenantId must be set.");
    }

    public object? GetValidatedUserId()
    {
        var id = GetCurrentUserId();
        if (id is Guid || id is int || id is null)
        {
            return id;
        }
        throw new InvalidOperationException("GetCurrentUserId must return a Guid, int");
    }

    public void ValidateConfiguration()
    {
        GetValidatedUserId();
        EnsureRequiredPropertiesSet();
    }
}

public class TenantBuilder
{
    private readonly IServiceCollection _services;
    private readonly TenantConfiguration _config;
    private readonly ILogger<TenantBuilder> _logger;

    public TenantBuilder(IServiceCollection services, ILogger<TenantBuilder> logger)
    {
        _services = services;
        _config = new TenantConfiguration();
        _logger = logger;
    }

    public TenantBuilder WithDbContext<TContext>() where TContext : DbContext
    {
        _config.DbContextType = typeof(TContext);
        return this;
    }

    public TenantBuilder WithUser()
    {
        _config.UserType = typeof(IdentityUser);
        return this;
    }

    public TenantBuilder WithUser<TUser>() where TUser : IdentityUser<Guid>
    {
        _config.UserType = typeof(TUser);
        return this;
    }

    public TenantBuilder WithRole()
    {
        _config.RoleType = typeof(IdentityRole);
        return this;
    }

    public TenantBuilder WithRole<TRole>() where TRole : IdentityRole<Guid>
    {
        _config.RoleType = typeof(TRole);
        return this;
    }

    public TenantBuilder WithCurrentUserProvider(Func<IServiceProvider, object> getCurrentUserId)
    {
        _config.GetCurrentUserId = () => getCurrentUserId(_services.BuildServiceProvider());
        return this;
    }

    public TenantBuilder WithCurrentUserTenantProvider(Func<IServiceProvider, Guid> getCurrentUserTenantId)
    {
        _config.GetCurrentUserTenantId = () => getCurrentUserTenantId(_services.BuildServiceProvider());
        return this;
    }

    public TenantBuilder WithoutTenantEntity()
    {
        _config.WithTenantEntity = false;
        return this;
    }

    internal TenantConfiguration Build()
    {
        _logger.LogInformation("Building tenant configuration...");
        _config.ValidateConfiguration();

        if (_config.GetCurrentUserTenantId == null)
        {
            _logger.LogInformation("No custom function provided for GetCurrentUserTenantId. Will default to Header 'X-Tenant-Id'.");
        }
        else
        {
            _logger.LogInformation("GetCurrentUserTenantId - using custom function.");
        }

        _services.AddSingleton<ITenantConfiguration>(_config);
        return _config;
    }
}
