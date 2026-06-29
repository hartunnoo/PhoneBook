using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Application.Services;
using PhoneBook.Common.Constants;
using PhoneBook.Components;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

// Application Services
builder.Services.AddScoped<ContactService>();

// HTTP Client for Blazor pre-render
builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    var client = new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
    return client;
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
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PhoneBook.Client._Imports).Assembly);

app.Run();
