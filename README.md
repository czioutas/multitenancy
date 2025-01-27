# Czioutas.Multitenancy

[![Test and Publish](https://github.com/czioutas/multitenancy/actions/workflows/publish.yml/badge.svg)](https://github.com/czioutas/multitenancy/actions/workflows/publish.yml)

A lightweight, flexible multi-tenancy library for .NET applications that makes it easy to add tenant isolation to your ASP.NET Core APIs using Entity Framework Core.

## Features

- Easy integration with ASP.NET Core and Entity Framework Core
- Automatic tenant context handling through middleware
- Built-in tenant isolation at the database level
- Support for tenant-aware entities
- Flexible tenant identification strategies
- Built-in tenant management API
- Exception handling specific to tenant operations
- Identity integration with custom tenant support

## Installation

Install the package via NuGet:

```bash
dotnet add package Czioutas.Multitenancy
```

## Quick Start

1. Add tenant configuration in your `Program.cs`:

```csharp
builder.Services.AddMultiTenancy<YourDbContext>(options =>
{
    options
        .WithDbContext<YourDbContext>()
        .WithUser<YourUser>()
        .WithRole<YourRole>()
        .WithCurrentUserProvider(sp => 
        {
            // Your logic to get current user ID
            return userId;
        })
        .WithCurrentUserTenantProvider(sp =>
        {
            // Your logic to get current tenant ID
            return tenantId;
        });
});
```

2. Add the middleware in your application pipeline:

```csharp
app.UseMultiTenancy();
```

3. Make your entities tenant-aware by inheriting from `TenantAwareEntity`:

```csharp
public class YourEntity : TenantAwareEntity
{
    public string Name { get; set; }
    // Your entity properties
}
```

## Configuration Options

### Tenant Identification

You can identify tenants through:
- Custom provider functions defined in your configuration
- HTTP Header "X-Tenant-Id" (used as default and fallback behavior)

### Database Context Setup

Your DbContext should inherit from `TenantIdentityDbContext`:

```csharp
public class YourDbContext : TenantIdentityDbContext<YourUser, YourRole, Guid>
{
    public YourDbContext(
        DbContextOptions options,
        IRequestTenant requestTenant)
        : base(options, requestTenant)
    {
    }
}
```

## Exception Handling

The library includes several specialized exceptions:
- `TenantNotFoundException`
- `TenantAlreadyExistsException`
- `TenantOperationException`

## API Reference

### ITenantService

```csharp
public interface ITenantService
{
    Task<TenantModel> CreateAsync(string tenantIdentifier);
    Task<TenantModel> FindByIdentifierAsync(string tenantIdentifier);
    Task<TenantModel> GetAsync(Guid tenantId);
    Task<TenantModel> GetAsync(string tenantIdentifier);
    string GetRandomIdentifier();
}
```

### IRequestTenant

```csharp
public interface IRequestTenant
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}
```

## Advanced Usage

### Custom Tenant Resolution

```csharp
builder.Services.AddMultiTenancy<YourDbContext>(options =>
{
    options.WithCurrentUserTenantProvider(sp =>
    {
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
        var user = httpContext?.User;
        
        // Your custom tenant resolution logic
        return tenantId;
    });
});
```

### Query Filtering

Tenant isolation is automatically applied to all entities implementing `ITenantAwareEntity`. The library ensures that queries only return data belonging to the current tenant.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.