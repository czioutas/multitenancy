using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Multitenancy;
using Multitenancy.Services;
using Multitenancy.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Multitenancy.Test;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddMultiTenancy<TestTenantIdentityDbContextSimple>(builder =>
{
    builder.WithDbContext<TestTenantIdentityDbContextSimple>()
              .WithUser()
              .WithRole()
           .WithCurrentUserProvider(sp =>
           {
               var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
               return httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
           })
            .WithCurrentUserTenantProvider(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var a = Guid.TryParse(httpContextAccessor.HttpContext?.Request.Headers["NotDefault-Tenant-Id"].FirstOrDefault(), out var b);
                return b;
            });
}, NullLogger<TenantBuilder>.Instance);

builder.Services.AddDbContext<TestTenantIdentityDbContextSimple>((provider, options) =>
{
    // options.UseNpgsql("Server=localhost;Port=5433;Database=applicationDb;User Id=applicationDb;Password=applicationDb");
    options.UseInMemoryDatabase("IntegrationTestingDb");
    options.EnableSensitiveDataLogging(false);
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<TestTenantIdentityDbContextSimple>()
.AddDefaultTokenProviders();

var app = builder.Build();

app.Urls.Add("http://localhost:7070");

app.UseRouting();
app.UseMiddleware<MultiTenantServiceMiddleware>();

app.MapGet("/api/v1/", async (TestTenantIdentityDbContextSimple dbContext, ITenantService tenantService, UserManager<IdentityUser> userManager) =>
{
    return Results.Ok("pong");
})
.WithName("GetRoot");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TestTenantIdentityDbContextSimple>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}

app.Run();