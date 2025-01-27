using Microsoft.AspNetCore.Http;
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
    Func<Guid> GetCurrentUserId { get; }
    Func<Guid> GetCurrentUserTenantId { get; }
    void SetTenantId(Guid tenantId);
}

public class TenantConfiguration : ITenantConfiguration
{
    private Type? _dbContextType;
    private Type? _userType;
    private Type? _roleType;
    private Func<Guid>? _getCurrentUserId;
    private Func<Guid>? _getCurrentUserTenantId;
    private Guid? _tenantId;

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

    public Type UserType
    {
        get
        {
            if (_userType == null)
            {
                throw new InvalidOperationException("UserType has not been set.");
            }
            return _userType;
        }
        set
        {
            _userType = value;
        }
    }

    public Type RoleType
    {
        get
        {
            if (_roleType == null)
            {
                throw new InvalidOperationException("RoleType has not been set.");
            }
            return _roleType;
        }
        set
        {
            _roleType = value;
        }
    }

    public Func<Guid> GetCurrentUserId
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
        if (UserType == null)
            throw new InvalidOperationException("UserType must be set.");
        if (RoleType == null)
            throw new InvalidOperationException("RoleType must be set.");
        if (GetCurrentUserId == null)
            throw new InvalidOperationException("GetCurrentUserId must be set.");
        if (GetCurrentUserTenantId == null)
            throw new InvalidOperationException("GetCurrentUserTenantId must be set.");
    }

    public void ValidateConfiguration()
    {
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

    public TenantBuilder WithUser<TUser>() where TUser : IdentityUser<Guid>
    {
        _config.UserType = typeof(TUser);
        return this;
    }

    public TenantBuilder WithRole<TRole>() where TRole : IdentityRole<Guid>
    {
        _config.RoleType = typeof(TRole);
        return this;
    }

    public TenantBuilder WithCurrentUserProvider(Func<IServiceProvider, Guid> getCurrentUserId)
    {
        _config.GetCurrentUserId = () => getCurrentUserId(_services.BuildServiceProvider());
        return this;
    }

    public TenantBuilder WithCurrentUserTenantProvider(Func<IServiceProvider, Guid> getCurrentUserTenantId)
    {
        _config.GetCurrentUserTenantId = () => getCurrentUserTenantId(_services.BuildServiceProvider());
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
