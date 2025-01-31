using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Multitenancy.Exceptions;
using Multitenancy.Services;

namespace Multitenancy.Test;

[TestClass]
public class TenantManagementTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestTenantIdentityDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly string _dbName;

    public TenantManagementTest()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = Guid.NewGuid().ToString();

        // Setup services
        var services = new ServiceCollection();

        services.AddSingleton(TimeProvider.System);

        // Add DbContext with in-memory database
        services.AddDbContext<TestTenantIdentityDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        // Setup mocks
        var loggerMock = new Mock<ILogger<TenantService>>();
        var builderLoggerMock = new Mock<ILogger<TenantBuilder>>();
        var requestTenantMock = new Mock<IRequestTenant>();

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

        // Add scoped mocks
        services.AddScoped(_ => requestTenantMock.Object);
        services.AddScoped(_ => loggerMock.Object);

        _serviceProvider = services.BuildServiceProvider();

        // Get instances
        _dbContext = _serviceProvider.GetRequiredService<TestTenantIdentityDbContext>();
        _tenantService = _serviceProvider.GetRequiredService<ITenantService>();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task CreateTenant_Success()
    {
        // Arrange
        var identifier = "tenant-1";

        // Act
        var result = await _tenantService.CreateAsync(identifier);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(identifier, result.Identifier);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreNotEqual(default, result.CreatedAt);
    }

    [TestMethod]
    public async Task CreateMultipleTenants_Success()
    {
        // Arrange
        var identifiers = new[] { "tenant-1", "tenant-2", "tenant-3" };

        // Act
        var createdTenants = await Task.WhenAll(
            identifiers.Select(id => _tenantService.CreateAsync(id))
        );

        // Assert
        Assert.AreEqual(identifiers.Length, createdTenants.Length);
        CollectionAssert.AllItemsAreUnique(createdTenants.Select(t => t.Id).ToArray());
        CollectionAssert.AllItemsAreUnique(createdTenants.Select(t => t.Identifier).ToArray());
        CollectionAssert.AreEquivalent(identifiers, createdTenants.Select(t => t.Identifier).ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(TenantAlreadyExistsException))]
    public async Task CreateTenant_DuplicateIdentifier_ThrowsException()
    {
        // Arrange
        var identifier = "duplicate-tenant";

        // Act
        await _tenantService.CreateAsync(identifier);
        await _tenantService.CreateAsync(identifier); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    public async Task GetTenant_ExistingTenant_Success()
    {
        // Arrange
        var identifier = "get-tenant-test";
        var created = await _tenantService.CreateAsync(identifier);

        // Act
        var result = await _tenantService.GetAsync(created.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(created.Identifier, result.Identifier);
    }

    [TestMethod]
    [ExpectedException(typeof(TenantNotFoundException))]
    public async Task GetTenant_NonExistentTenant_ThrowsException()
    {
        // Act
        await _tenantService.GetAsync(Guid.NewGuid()); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    public async Task UpdateTenant_Success()
    {
        // Arrange
        var originalIdentifier = "update-tenant-test";
        var newIdentifier = "updated-tenant-test";
        var created = await _tenantService.CreateAsync(originalIdentifier);

        // Act
        var result = await _tenantService.UpdateAsync(created.Id, newIdentifier);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(created.Id, result.Id);
        Assert.AreEqual(newIdentifier, result.Identifier);
        Assert.AreEqual(created.CreatedAt, result.CreatedAt);
        Assert.IsNotNull(result.UpdatedAt);
        Assert.IsFalse(result.Deleted);

        // Verify we can get the tenant with new identifier
        var retrieved = await _tenantService.GetAsync(newIdentifier);
        Assert.AreEqual(newIdentifier, retrieved.Identifier);
    }

    [TestMethod]
    [ExpectedException(typeof(TenantNotFoundException))]
    public async Task UpdateTenant_NonExistentTenant_ThrowsException()
    {
        // Act
        await _tenantService.UpdateAsync(Guid.NewGuid(), "new-identifier"); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    [ExpectedException(typeof(TenantAlreadyExistsException))]
    public async Task UpdateTenant_DuplicateIdentifier_ThrowsException()
    {
        // Arrange
        var tenant1 = await _tenantService.CreateAsync("tenant-1");
        var tenant2 = await _tenantService.CreateAsync("tenant-2");

        // Act
        await _tenantService.UpdateAsync(tenant2.Id, "tenant-1"); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    [ExpectedException(typeof(TenantNotFoundException))]
    public async Task UpdateTenant_DeletedTenant_ThrowsException()
    {
        // Arrange
        var tenant = await _tenantService.CreateAsync("to-be-deleted");
        await _tenantService.DeleteAsync(tenant.Id);

        // Act
        await _tenantService.UpdateAsync(tenant.Id, "new-identifier"); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    public async Task DeleteTenant_Success()
    {
        // Arrange
        var tenant = await _tenantService.CreateAsync("delete-test");

        // Act
        var result = await _tenantService.DeleteAsync(tenant.Id);

        // Assert
        Assert.IsTrue(result);

        // Verify tenant can't be retrieved after deletion
        await Assert.ThrowsExceptionAsync<TenantNotFoundException>(
            async () => await _tenantService.GetAsync(tenant.Id)
        );
    }

    [TestMethod]
    [ExpectedException(typeof(TenantNotFoundException))]
    public async Task DeleteTenant_NonExistentTenant_ThrowsException()
    {
        // Act
        await _tenantService.DeleteAsync(Guid.NewGuid()); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    [ExpectedException(typeof(TenantNotFoundException))]
    public async Task DeleteTenant_AlreadyDeleted_ThrowsException()
    {
        // Arrange
        var tenant = await _tenantService.CreateAsync("double-delete-test");
        await _tenantService.DeleteAsync(tenant.Id);

        // Act
        await _tenantService.DeleteAsync(tenant.Id); // Should throw

        // Assert - handled by ExpectedException attribute
    }

    [TestMethod]
    public async Task DeleteTenant_VerifyQueriesExcludeDeleted()
    {
        // Arrange
        var identifier = "exclude-deleted-test";
        var tenant = await _tenantService.CreateAsync(identifier);

        // Act
        await _tenantService.DeleteAsync(tenant.Id);

        // Assert - Verify different query methods all exclude the deleted tenant
        await Assert.ThrowsExceptionAsync<TenantNotFoundException>(
            async () => await _tenantService.GetAsync(tenant.Id)
        );

        await Assert.ThrowsExceptionAsync<TenantNotFoundException>(
            async () => await _tenantService.GetAsync(identifier)
        );

        await Assert.ThrowsExceptionAsync<TenantNotFoundException>(
            async () => await _tenantService.FindByIdentifierAsync(identifier)
        );
    }
}