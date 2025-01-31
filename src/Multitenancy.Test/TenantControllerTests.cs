using System.Net;
using System.Text;
using System.Text.Json;
using Multitenancy.Models;

namespace Multitenancy.Test;

[TestClass]
public class TenantControllerTests
{
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        var factory = new TestApplicationFactory<Program>()
            .WithWebHostBuilder(builder => { });
        _client = factory.CreateClient();
        _client.BaseAddress = new Uri("http://localhost:7070/api/v1/");
    }

    [TestMethod]
    public async Task PingApi_ShouldReturn200WithPongMessage()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("\"pong\"", content);
    }

    [TestMethod]
    public async Task GetTenant_ShouldReturn200_WithTenantData()
    {
        // Arrange
        var userId = "userId";
        var identifier = "test-tenant";

        // Create tenant first
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "tenant");
        createRequest.Headers.Add("X-User-Id", userId);
        createRequest.Content = new StringContent(JsonSerializer.Serialize(identifier), Encoding.UTF8, "application/json");
        var createResponse = await _client.SendAsync(createRequest);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var createdTenant = JsonSerializer.Deserialize<TenantModel>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Get tenant
        var request = new HttpRequestMessage(HttpMethod.Get, "tenant");
        request.Headers.Add("X-User-Id", userId);
        request.Headers.Add("NotDefault-Tenant-Id", createdTenant.Id.ToString());
        var response = await _client.SendAsync(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TenantModel>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(result);
        Assert.AreEqual(identifier, result.Identifier);
    }

    [TestMethod]
    public async Task GetTenant_ShouldReturn404_WhenTenantNotFound()
    {
        // Arrange
        // We are not passing any headers. So, tenant will not be found.
        // Act
        var response = await _client.GetAsync("/api/v1/tenant");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTenant_ShouldReturn200_WithUpdatedTenant()
    {
        // Arrange
        var userId = "userId";
        var identifier = "test-tenant";

        // Create tenant first
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "tenant");
        createRequest.Headers.Add("X-User-Id", userId);
        createRequest.Content = new StringContent(JsonSerializer.Serialize(identifier), Encoding.UTF8, "application/json");
        var createResponse = await _client.SendAsync(createRequest);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var createdTenant = JsonSerializer.Deserialize<TenantModel>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"tenant/{createdTenant.Id}");
        updateRequest.Headers.Add("X-User-Id", userId);
        updateRequest.Headers.Add("NotDefault-Tenant-Id", createdTenant.Id.ToString());
        updateRequest.Content = new StringContent(
           JsonSerializer.Serialize("NewIdentifier"),
           Encoding.UTF8,
           "application/json"
        );
        var response = await _client.SendAsync(updateRequest);
        var updatedTenant = JsonSerializer.Deserialize<TenantModel>(
           await response.Content.ReadAsStringAsync(),
           new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.AreEqual(updatedTenant.Identifier, "NewIdentifier");
    }

    [TestMethod]
    public async Task UpdateTenant_ShouldReturn400_WhenIdentifierExists()
    {
        // Arrange
        var userId = "userId";
        var identifier = "test-tenant";

        // Create first tenant
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "tenant");
        createRequest.Headers.Add("X-User-Id", userId);
        createRequest.Content = new StringContent(JsonSerializer.Serialize(identifier), Encoding.UTF8, "application/json");
        var createResponse = await _client.SendAsync(createRequest);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var createdTenant = JsonSerializer.Deserialize<TenantModel>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var createRequestTenant2 = new HttpRequestMessage(HttpMethod.Post, "tenant");
        createRequestTenant2.Headers.Add("X-User-Id", userId);
        createRequestTenant2.Content = new StringContent(JsonSerializer.Serialize(identifier + "Tenant2"), Encoding.UTF8, "application/json");
        var createResponseTenant2 = await _client.SendAsync(createRequestTenant2);
        var responseContentTenant2 = await createResponseTenant2.Content.ReadAsStringAsync();
        // var createdTenantTenant2 = JsonSerializer.Deserialize<TenantModel>(responseContentTenant2, new JsonSerializerOptions
        // {
        //     PropertyNameCaseInsensitive = true
        // });

        // Act
        // We will try to update the identifier of Tenant 1 to the same as Tenant 2
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"tenant/{createdTenant.Id}");
        updateRequest.Headers.Add("X-User-Id", userId);
        updateRequest.Headers.Add("NotDefault-Tenant-Id", createdTenant.Id.ToString());
        updateRequest.Content = new StringContent(
           JsonSerializer.Serialize(identifier + "Tenant2"),
           Encoding.UTF8,
           "application/json"
        );
        var response = await _client.SendAsync(updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

