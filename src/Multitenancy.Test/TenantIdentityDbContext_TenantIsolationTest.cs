using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Multitenancy.Services;
using Multitenancy.Test.Fixtures;

namespace Multitenancy.Test;

[TestClass]
public class TenantIdentityDbContext_TenantIsolationTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestTenantIdentityDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IRequestTenant _requestTenant;
    private readonly string _dbName;

    public TenantIdentityDbContext_TenantIsolationTest()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        // Initialize RequestTenant first
        _requestTenant = new RequestTenant();

        services.AddSingleton(TimeProvider.System);

        // Add DbContext with in-memory database and pass the RequestTenant
        services.AddDbContext<TestTenantIdentityDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        // Setup mocks
        var loggerMock = new Mock<ILogger<TenantService>>();
        var builderLoggerMock = new Mock<ILogger<TenantBuilder>>();

        // Configure tenant services
        services.AddMultiTenancy<TestTenantIdentityDbContext>(options =>
        {
            options
                .WithDbContext<TestTenantIdentityDbContext>()
                .WithUser<Microsoft.AspNetCore.Identity.IdentityUser<Guid>>()
                .WithRole<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
                .WithCurrentUserProvider(_ => Guid.NewGuid())
                .WithCurrentUserTenantProvider(_ => Guid.NewGuid());
        }, builderLoggerMock.Object);

        // Register the initialized RequestTenant
        services.AddScoped<IRequestTenant>(_ => _requestTenant);
        services.AddScoped(_ => loggerMock.Object);

        _serviceProvider = services.BuildServiceProvider();

        // Initialize DbContext with the RequestTenant
        _dbContext = new TestTenantIdentityDbContext(
            _serviceProvider.GetRequiredService<DbContextOptions<TestTenantIdentityDbContext>>(),
            _requestTenant,
            TimeProvider.System,
            _serviceProvider.GetRequiredService<ITenantConfiguration>()
        );

        _tenantService = _serviceProvider.GetRequiredService<ITenantService>();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task QueryReturnsOnlyCurrentTenantData()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        // Create resources for tenant 1
        _requestTenant.SetTenantId(tenant1Id);
        await _dbContext.DemoResources.AddRangeAsync(
        [
            new DemoResourceEntity { Name = "Resource1-T1" },
            new DemoResourceEntity { Name = "Resource2-T1" }
        ]);
        await _dbContext.SaveChangesAsync();

        // Create resources for tenant 2
        _requestTenant.SetTenantId(tenant2Id);
        await _dbContext.DemoResources.AddRangeAsync(
        [
            new DemoResourceEntity { Name = "Resource1-T2" },
            new DemoResourceEntity { Name = "Resource2-T2" }
        ]);
        await _dbContext.SaveChangesAsync();

        // Act - Query as tenant 1
        // Tenant will be 1 as this was the one set during the filter apply.
        var tenant1Resources = await _dbContext.DemoResources.ToListAsync();

        // Assert
        Assert.AreEqual(2, tenant1Resources.Count(r => r.TenantId == tenant1Id), "Should have 2 resources for tenant 1");
        Assert.IsTrue(tenant1Resources.All(r => r.TenantId == tenant1Id), "All resources should belong to tenant 1");
        Assert.IsTrue(tenant1Resources.All(r => r.Name.EndsWith("-T1")), "All resources should have T1 suffix");
    }

    [TestMethod]
    public async Task AutomaticTenantIdAssignment()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _requestTenant.SetTenantId(tenantId);

        // Act - Create resource without explicitly setting TenantId
        var resource = new DemoResourceEntity { Name = "AutoAssignedTenant" };
        await _dbContext.DemoResources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedResource = await _dbContext.DemoResources.FirstAsync();
        Assert.AreEqual(tenantId, savedResource.TenantId);
    }
}
