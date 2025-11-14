using AspNetCoreRateLimit;
using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Repositories;
using LoginandRegisterMVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework with retry policy and SSL/TLS configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: new[] { 2, 53, 121, 232, 258, 1205 });
        sqlOptions.CommandTimeout(600);
    }));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Users/Login";
        options.LogoutPath = "/Users/Logout";
        options.AccessDeniedPath = "/Users/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services
// SECURITY FIX: New secure password hashing service (PBKDF2)
builder.Services.AddScoped<ISecurePasswordHashService, SecurePasswordHashService>();

// LEGACY: Keep old SHA1 service temporarily for backward compatibility during migration
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
#pragma warning restore CS0618

// Repository Pattern: Add user repository for data access abstraction
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Service Layer: Add business logic services
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<IUserService, UserService>();

// SECURITY FIX: Add rate limiting to prevent brute force attacks
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add runtime compilation for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// SECURITY FIX: Enable rate limiting middleware
app.UseIpRateLimiting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Login}/{id?}");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
