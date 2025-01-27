using Multitenancy.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Multitenancy.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Multitenancy;

public static class MultiTenancyExtensions
{
    public static IServiceCollection AddMultiTenancy<TContext>(
    this IServiceCollection services,
    Action<TenantBuilder> configure,
    ILogger<TenantBuilder> logger) where TContext : DbContext
    {
        // Create the TenantBuilder with the provided logger
        var tenantBuilder = new TenantBuilder(services, logger);

        // Configure the TenantBuilder
        configure(tenantBuilder);

        // Build the configuration
        var config = tenantBuilder.Build();

        // Register services in the IServiceCollection
        services.AddSingleton<ITenantConfiguration>(config);
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IRequestTenant, RequestTenant>();
        services.AddTransient<MultiTenantServiceMiddleware>();

        services.AddMvcCore()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new AssemblyPart(typeof(MultiTenancyExtensions).Assembly));
            });

        return services;
    }

    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<MultiTenantServiceMiddleware>();

        return app;
    }
}