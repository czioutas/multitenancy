using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Multitenancy.Services;
using Multitenancy.Test.Fixtures;

namespace Multitenancy.Test;

[TestClass]
public class TenantIsolationTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IRequestTenant _requestTenant;
    private readonly string _dbName;

    public TenantIsolationTest()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        // Initialize RequestTenant first
        _requestTenant = new RequestTenant();

        services.AddSingleton(TimeProvider.System);

        // Add DbContext with in-memory database and pass the RequestTenant
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        // Setup mocks
        var loggerMock = new Mock<ILogger<TenantService>>();
        var builderLoggerMock = new Mock<ILogger<TenantBuilder>>();

        // Configure tenant services
        services.AddMultiTenancy<TestDbContext>(options =>
        {
            options
                .WithDbContext<TestDbContext>()
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
        _dbContext = new TestDbContext(
            _serviceProvider.GetRequiredService<DbContextOptions<TestDbContext>>(),
            _requestTenant,
            TimeProvider.System
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

        // Debug: Let's see all resources without filters
        var allResources = await _dbContext.DemoResources.IgnoreQueryFilters().ToListAsync();
        Console.WriteLine($"Total resources: {allResources.Count}");
        foreach (var resource in allResources)
        {
            Console.WriteLine($"Resource: {resource.Name}, TenantId: {resource.TenantId}");
        }

        // Act - Query as tenant 1
        _requestTenant.SetTenantId(tenant1Id);
        var tenant1Resources = await _dbContext.DemoResources.ToListAsync();

        // Assert
        Assert.AreEqual(2, tenant1Resources.Count, "Should have 2 resources for tenant 1");
        Assert.IsTrue(tenant1Resources.All(r => r.TenantId == tenant1Id), "All resources should belong to tenant 1");
        Assert.IsTrue(tenant1Resources.All(r => r.Name.EndsWith("-T1")), "All resources should have T1 suffix");
    }
    [TestMethod]
    public async Task CrossTenantAccessPrevented()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        // Create a resource for tenant 1
        _requestTenant.SetTenantId(tenant1Id);
        var resource = new DemoResourceEntity { Name = "Tenant1Resource", TenantId = tenant1Id };
        await _dbContext.DemoResources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();

        // Act - Try to access as tenant 2
        _requestTenant.SetTenantId(tenant2Id);
        var tenant2Resources = await _dbContext.DemoResources.ToListAsync();

        // Assert
        Assert.AreEqual(0, tenant2Resources.Count);
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

    [TestMethod]
    public async Task TenantSwitching()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        // Act & Assert - Tenant 1
        _requestTenant.SetTenantId(tenant1Id);
        await _dbContext.DemoResources.AddAsync(new DemoResourceEntity
        {
            Name = "Tenant1Resource"
        });
        await _dbContext.SaveChangesAsync();

        var tenant1Resources = await _dbContext.DemoResources.ToListAsync();
        Assert.AreEqual(1, tenant1Resources.Count);

        // Switch to Tenant 2
        _requestTenant.SetTenantId(tenant2Id);
        await _dbContext.DemoResources.AddAsync(new DemoResourceEntity
        {
            Name = "Tenant2Resource"
        });
        await _dbContext.SaveChangesAsync();

        var tenant2Resources = await _dbContext.DemoResources.ToListAsync();
        Assert.AreEqual(1, tenant2Resources.Count);
        Assert.AreNotEqual(tenant1Resources[0].Id, tenant2Resources[0].Id);
    }
}