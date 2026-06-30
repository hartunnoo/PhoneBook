using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Services;
using PhoneBook.Common.Constants;
using PhoneBook.Components;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Repositories;
using PhoneBook.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("App", "PhoneBook")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/phonebook-.txt", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Database + Identity
builder.Services.AddDbContext<PhoneBookDbContext>(options =>
    options.UseSqlite($"Data Source={DbConfig.ConnectionString}"));

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
    .AddEntityFrameworkStores<PhoneBookDbContext>()
    .AddSignInManager();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();

// API returns 401 JSON instead of redirecting to login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Unit of Work & Repository
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// AI Services
builder.Services.AddHttpClient<DeepSeekService>();
builder.Services.AddScoped<DeepSeekService>();

// RLS - Row Level Security
builder.Services.AddScoped<RowLevelSecurityService>();

// Application Services
builder.Services.AddScoped<ContactService>();

// HTTP Client for Blazor (only for SSR pre-render)
builder.Services.AddScoped<HttpClient>(sp =>
{
    try
    {
        var nav = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
    }
    catch { return new HttpClient(); }
});

// Controllers
builder.Services.AddControllers();

// Blazor WASM
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Auto-create database (ensure tables exist)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PhoneBookDbContext>();
    db.Database.EnsureCreated();

    // Seed RLS access grants
    var rls = scope.ServiceProvider.GetRequiredService<RowLevelSecurityService>();
    await rls.GrantAccessAsync(AppConstants.RlsAdminUser, isAdmin: true);
    foreach (var ministry in AppConstants.RlsDefaultMinistries)
        await rls.GrantAccessAsync(AppConstants.RlsDefaultUser, kementerian: ministry);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
app.UseAntiforgery();

app.UseStaticFiles();  // Serve wwwroot (photos, js)
app.MapStaticAssets();  // Blazor fingerprinted assets

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PhoneBook.Client._Imports).Assembly);

try { app.Run(); }
finally { Log.CloseAndFlush(); }
