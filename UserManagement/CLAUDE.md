# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a migrated ASP.NET Core MVC 8 (.NET 8) user management application, originally from .NET Framework 4.7.2 with ASP.NET MVC 5. The application demonstrates user registration, login, and role-based authorization with cookie authentication.

**Key migration decisions:**
- Preserved SHA1 password hashing (PasswordHashService) for backward compatibility with legacy database
- Converted Forms Authentication to Cookie Authentication
- Migrated EF 6 to EF Core 8 with all migration history preserved
- Maintained 100% UI fidelity with Bootstrap 4.5.2

## Common Commands

### Build & Run
```bash
cd UserManagement/src/LoginandRegisterMVC
dotnet build
dotnet run
```

### Database Management
```bash
cd UserManagement/src/LoginandRegisterMVC

# Apply migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add <MigrationName>

# Remove last migration
dotnet ef migrations remove
```

### Testing
```bash
# Run all tests
cd UserManagement
dotnet test

# Run unit tests only
cd UserManagement/tests/LoginandRegisterMVC.UnitTests
dotnet test

# Run integration tests only
cd UserManagement/tests/LoginandRegisterMVC.IntegrationTests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~UsersControllerTests.Login_ValidCredentials_RedirectsToIndex"
```

### Watch Mode (for development)
```bash
cd UserManagement/src/LoginandRegisterMVC
dotnet watch run
```

## Architecture

### Application Structure
- **Single-layer MVC architecture** with no separate business/repository layers
- Controllers directly inject `UserContext` (DbContext) and services
- **Primary constructor syntax** used throughout (C# 12 feature)

### Authentication Flow
1. Cookie Authentication configured in Program.cs with 60-minute sliding expiration
2. Login creates claims-based identity with ClaimTypes.Role for authorization
3. Session stores UserId, Username, and Role redundantly (preserved from legacy)
4. Admin user auto-seeded on first login page access (admin@demo.com / Admin@123)

### Database Context
- `UserContext` is the single DbContext with `Users` DbSet
- Connection string in appsettings.json includes retry policy configuration (10 retries, 60s max delay)
- TrustServerCertificate=true handles SQL Server SSL/TLS certificates
- Entity configurations done in OnModelCreating (not separate configuration classes)

### Password Hashing
- `IPasswordHashService` / `PasswordHashService` registered as scoped service
- **Important:** Uses legacy SHA1 hashing (not bcrypt/PBKDF2) for database compatibility
- Hash format: ASCII bytes -> SHA1 -> concatenated byte values as string

### Dependency Injection
Services are registered in Program.cs:
- `AddDbContext<UserContext>` - EF Core context with SQL Server
- `AddAuthentication` / `AddCookie` - Cookie authentication
- `AddSession` - Session state
- `AddScoped<IPasswordHashService, PasswordHashService>` - Password hashing service
- `AddRazorRuntimeCompilation` - Only in Development for view hot-reload

### Testing Architecture
- **Unit tests:** Mock DbContext using Moq and InMemory provider
- **Integration tests:** Use WebApplicationFactory<Program> with test server
- Test projects reference main project via ProjectReference
- NUnit framework with async test support

## Configuration

### Connection String
Located in `appsettings.json`:
- Must include `TrustServerCertificate=true` for SQL Server connections
- Default database: `db_MigratedLginMVC_13_1`
- Update Server, user id, and password for your environment

### Default Route
`{controller=Users}/{action=Login}/{id?}` - App starts at login page

## Important Implementation Notes

### Model Validation
- User model includes both Password and ConfirmPassword properties
- Both are hashed before saving to database (legacy behavior)
- Email (UserId) is the primary key with MaxLength(128)

### Authorization Patterns
- `[Authorize]` attribute on Index and Logout actions
- Role-based authorization via `ClaimTypes.Role` claim
- Login/Register pages are public (no [Authorize])

### Migration History
The Migrations folder contains converted migrations from EF 6:
- Original: 20201007183518_InitialCreate, 20201007183813_relatin
- New: 20250909171418_CreateUsersTable
- Do not delete old migrations - they preserve database schema evolution

### Views & Static Files
- Razor views in Views folder with _Layout.cshtml shared layout
- Bootstrap 4.5.2, jQuery 3.5.1, and validation libraries in wwwroot/js
- jQuery validation configured for client-side model validation

### Controllers Pattern
- Use primary constructors for dependency injection (C# 12):
  ```csharp
  public class UsersController(UserContext context, IPasswordHashService passwordHashService) : Controller
  ```
- All database operations use async/await pattern
- Controllers directly access DbContext (no repository pattern)
- Anti-forgery tokens required on all POST actions via `[ValidateAntiForgeryToken]`

### UsersController Specifics
Location: `Controllers/UsersController.cs`

**Auto-seeding behavior:**
- Admin user is created automatically on first login page GET request
- Check happens in `Login()` GET action, not in Program.cs startup
- Default admin: admin@demo.com / Admin@123 / Admin role

**Registration flow:**
- Checks if user already exists by UserId (email)
- Hashes BOTH Password and ConfirmPassword fields before saving
- Redirects to Index after successful registration

**Login flow:**
- Hashes incoming password and queries database for match
- Creates claims: Name (UserId), NameIdentifier (UserId), "Username" (Username), Role (Role)
- Signs in with CookieAuthenticationDefaults.AuthenticationScheme
- Stores UserId, Username, Role in session (redundancy from legacy)
- Redirects to Index on success

### Database Configuration Details
In `Program.cs`, DbContext is configured with:
- **Retry policy**: 10 max retries, 60s max delay
- **Retry on error numbers**: 2, 53, 121, 232, 258, 1205 (connection and deadlock errors)
- **Command timeout**: 600 seconds
- **Connection pooling**: Max 200 connections (in connection string)

### Session Configuration
- 60-minute idle timeout
- HttpOnly cookies
- IsEssential = true (works without consent)

### URLs and Ports
Development server runs on:
- HTTPS: `https://localhost:62406`
- HTTP: `http://localhost:62407`
- Default route redirects to `/Users/Login`

## Analytics and Logging

### Current State
**No analytics system implemented.** The application only uses ASP.NET Core's built-in logging framework.

**Session tracking** exists but is for authentication only (UserId, Username, Role stored in session).

### Logging Configuration
In `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

EF Core SQL command logging is enabled in Development to see generated queries in console.

### Future Analytics Integration Points
If implementing analytics:
- Add scripts in `Views/Shared/_Layout.cshtml` (before `</body>`)
- Register analytics services in `Program.cs` DI container
- Track events in controller actions (login, registration, logout)
- Consider Application Insights, Google Analytics 4, or structured logging with Serilog

## Security Notes

### Known Security Considerations
1. **SHA1 Password Hashing**: Cryptographically weak by modern standards. Preserved for backward compatibility with migrated database. For new projects, use ASP.NET Core Identity with PBKDF2/bcrypt/Argon2.

2. **No Email Verification**: Users can register with any email address without verification.

3. **No Rate Limiting**: Login endpoint is vulnerable to brute force attacks.

4. **No Password Complexity Requirements**: Weak passwords are accepted.

5. **Connection String in appsettings.json**: Contains SQL Server credentials. Use environment variables or Azure Key Vault in production.

### Implemented Security Measures
- Cookie authentication with HttpOnly flag
- Anti-forgery tokens on all POST forms (`@Html.AntiForgeryToken()`)
- Authorization via `[Authorize]` attribute
- EF Core parameterized queries prevent SQL injection
- HTTPS enforced in production (HSTS)

## Key File Locations

### Core Application Files
- `Program.cs` - Application entry point, DI configuration, middleware pipeline
- `Controllers/UsersController.cs` - Authentication and user management (auto-seeds admin)
- `Controllers/HomeController.cs` - Basic pages (Index, About, Contact, Error)
- `Data/UserContext.cs` - EF Core DbContext with Users DbSet
- `Models/User.cs` - User entity (UserId, Username, Password, ConfirmPassword, Role)
- `Services/PasswordHashService.cs` - SHA1 password hashing implementation

### Configuration Files
- `appsettings.json` - Production configuration (connection string, logging)
- `appsettings.Development.json` - Development overrides
- `Properties/launchSettings.json` - Development server configuration (ports 62406/62407)

### Views
- `Views/Shared/_Layout.cshtml` - Master layout with Bootstrap navbar
- `Views/Users/Login.cshtml` - Login form
- `Views/Users/Register.cshtml` - Registration form
- `Views/Users/Index.cshtml` - User list (authorized only)

### Tests
- `tests/LoginandRegisterMVC.UnitTests/` - Unit tests with mocked dependencies
- `tests/LoginandRegisterMVC.IntegrationTests/` - Integration tests with test server
