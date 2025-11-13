# Application Architecture Documentation

**Project**: User Management Application (ASP.NET Core MVC 8)
**Last Updated**: 2025-11-13
**Version**: 1.0

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Project Structure](#project-structure)
3. [Technology Stack](#technology-stack)
4. [Architectural Patterns](#architectural-patterns)
5. [Application Entry Points](#application-entry-points)
6. [Key Components](#key-components)
7. [Data Architecture](#data-architecture)
8. [Build & Deployment](#build--deployment)
9. [Dependencies](#dependencies)
10. [Testing Architecture](#testing-architecture)
11. [Security Architecture](#security-architecture)
12. [Migration Context](#migration-context)
13. [Configuration](#configuration)
14. [Analytics & Logging](#analytics--logging)

---

## Project Overview

This is a **modernized ASP.NET Core MVC 8 user management application** successfully migrated from .NET Framework 4.7.2. It provides user registration, authentication, and basic user management functionality with 100% UI fidelity to the legacy system.

**Root Directory**: `D:\Projects\AI_Project_Conversion\Claude_Code`

**Key Characteristics**:
- Monolithic MVC architecture
- Direct data access pattern (no repository layer)
- Cookie-based authentication
- Entity Framework Core with SQL Server
- Comprehensive test coverage (NUnit)

---

## Project Structure

```
Claude_Code/
├── .claude/                           # Claude Code configuration
│   └── settings.local.json           # Permissions and local settings
├── Documents/                         # Project documentation
│   ├── Claude.md                     # Quick reference guide
│   └── architecture.md               # This file
├── Planning/                          # Planning artifacts (empty)
├── Prompts/                          # Prompt templates (empty)
└── UserManagement/                   # Main application directory
    ├── LoginandRegisterMVC.sln       # Visual Studio solution file
    ├── README.md                     # Project overview
    ├── CLAUDE.md                     # Detailed development guide
    ├── src/                          # Source code
    │   └── LoginandRegisterMVC/      # Main web application
    │       ├── Controllers/          # MVC Controllers
    │       │   ├── UsersController.cs
    │       │   └── HomeController.cs
    │       ├── Data/                 # DbContext & data access
    │       │   └── UserContext.cs
    │       ├── Models/               # Domain models
    │       │   └── User.cs
    │       ├── Services/             # Business services
    │       │   ├── IPasswordHashService.cs
    │       │   └── PasswordHashService.cs
    │       ├── Views/                # Razor views
    │       │   ├── Shared/
    │       │   ├── Users/
    │       │   └── Home/
    │       ├── Migrations/           # EF Core migrations
    │       ├── wwwroot/              # Static assets
    │       │   ├── css/
    │       │   ├── js/
    │       │   └── lib/
    │       ├── Properties/           # Launch settings
    │       ├── Program.cs            # Application entry point
    │       ├── appsettings.json      # Configuration
    │       └── LoginandRegisterMVC.csproj
    └── tests/                        # Test projects
        ├── LoginandRegisterMVC.UnitTests/
        │   ├── Controllers/
        │   │   ├── UsersControllerTests.cs
        │   │   └── HomeControllerTests.cs
        │   └── LoginandRegisterMVC.UnitTests.csproj
        └── LoginandRegisterMVC.IntegrationTests/
            ├── Controllers/
            │   ├── UsersControllerIntegrationTests.cs
            │   └── HomeControllerIntegrationTests.cs
            └── LoginandRegisterMVC.IntegrationTests.csproj
```

---

## Technology Stack

### Framework & Runtime
- **.NET 8** (Target Framework: net8.0)
- **ASP.NET Core MVC 8** (Web framework)
- **C# 12** (Language features: primary constructors, nullable reference types)

### Database & ORM
- **SQL Server** (Database engine)
- **Entity Framework Core 8.0.10** (ORM)
- **EF Core Migrations** (Database schema versioning)

### Authentication & Session
- **Cookie Authentication** (Microsoft.AspNetCore.Authentication.Cookies 2.2.0)
- **Claims-based Identity** (ClaimTypes.Name, Role, NameIdentifier)
- **Session State** (ASP.NET Core Session with 60-minute timeout)

### Frontend
- **Razor Views** (Server-side rendering)
- **Bootstrap 4.5.2** (CSS framework)
- **jQuery 3.5.1** (JavaScript library)
- **jQuery Validation** (Client-side form validation)
- **Modernizr 2.8.3** (Feature detection)

### Testing
- **NUnit 4.0.1** (Testing framework)
- **Moq 4.20.69** (Mocking framework)
- **Microsoft.AspNetCore.Mvc.Testing 8.0.10** (Integration testing)
- **Microsoft.EntityFrameworkCore.InMemory 8.0.10** (In-memory database for tests)
- **Coverlet** (Code coverage)

### Development Tools
- **Razor Runtime Compilation** (Development hot-reload)
- **Visual Studio Launch Settings** (Development server configuration)

---

## Architectural Patterns

### Overall Architecture
**Pattern**: Monolithic MVC Application (Single-layer architecture)

### Design Pattern
**Direct Data Access Pattern**
- NO Repository Pattern
- NO Separate Business Logic Layer
- Controllers directly inject `UserContext` (DbContext)
- Services layer only for cross-cutting concerns (password hashing)

### Key Architectural Characteristics

#### 1. Simplified MVC Pattern
- **Controllers**: Handle both UI logic and data access
- **Models**: POCOs with data annotations for validation
- **Views**: Razor templates with inline client-side validation

#### 2. Dependency Injection
- Built-in ASP.NET Core DI container
- Services registered in `Program.cs`
- Primary constructor injection pattern (C# 12)

#### 3. Middleware Pipeline
Configured in `Program.cs` with the following order:
```
Exception Handler → HTTPS Redirection → Static Files →
Routing → Session → Authentication → Authorization → Controllers
```

#### 4. Authentication Architecture
- **Cookie-based authentication** (primary mechanism)
- **Claims-based authorization** (Role claims)
- **Session redundancy** (legacy compatibility)
- **Auto-seeding** of admin user on first access

#### 5. Data Access Pattern
- Code-First Entity Framework Core
- Single DbContext (`UserContext`)
- Async/await throughout
- Connection retry policy with transient fault handling
- No caching layer (direct database access)

---

## Application Entry Points

### Main Entry Point
**File**: `D:\Projects\AI_Project_Conversion\Claude_Code\UserManagement\src\LoginandRegisterMVC\Program.cs`

### Application Initialization Flow

```csharp
1. WebApplication.CreateBuilder(args)
   ↓
2. Service Registration:
   - AddControllersWithViews()
   - AddDbContext<UserContext>(SQL Server + Retry Policy)
   - AddAuthentication(Cookie) + AddCookie
   - AddAuthorization()
   - AddSession()
   - AddScoped<IPasswordHashService, PasswordHashService>()
   - AddRazorRuntimeCompilation() [Development only]
   ↓
3. Middleware Pipeline Configuration:
   - UseExceptionHandler("/Home/Error") [Production]
   - UseHsts() [Production]
   - UseHttpsRedirection()
   - UseStaticFiles()
   - UseRouting()
   - UseSession()
   - UseAuthentication()
   - UseAuthorization()
   ↓
4. Route Configuration:
   - Default: "{controller=Users}/{action=Login}/{id?}"
   ↓
5. app.Run()
```

### Default Route
Application starts at: `/Users/Login`

### Development URLs
- **HTTPS**: `https://localhost:62406`
- **HTTP**: `http://localhost:62407`

### Partial Program Class
```csharp
public partial class Program { }
```
This allows test projects to access the Program class for integration testing.

---

## Key Components

### Controllers

#### UsersController
**File**: `Controllers/UsersController.cs`

**Purpose**: User authentication and management

**Dependencies**:
- `UserContext` (DbContext)
- `IPasswordHashService`

**Actions**:
- `Index()` [GET, Authorized] - Lists all users
- `Register()` [GET] - Registration form
- `Register(User)` [POST] - Process registration
- `Login()` [GET] - Login form + admin auto-seeding
- `Login(User)` [POST] - Authenticate user
- `Logout()` [GET, Authorized] - Sign out

**Key Behaviors**:
- Admin auto-seeding on first login page load
- Both `Password` and `ConfirmPassword` hashed before save (legacy behavior)
- Dual authentication: Cookie auth + Session storage
- Claims created: Name, NameIdentifier, Username, Role

#### HomeController
**File**: `Controllers/HomeController.cs`

**Purpose**: Basic informational pages

**Actions**: Index, About, Contact, Error

**Authorization**: Public (no [Authorize] required)

### Models

#### User Model
**File**: `Models/User.cs`

```csharp
public class User
{
    [Key, Required, DataType(EmailAddress)]
    public string UserId { get; set; }  // Email, max 128 chars, Primary Key

    [Required, Display(Name = "Username")]
    public string Username { get; set; }

    [Required, DataType(Password)]
    public string Password { get; set; }  // SHA1 hashed

    [NotMapped, Compare("Password"), DataType(Password)]
    public string ConfirmPassword { get; set; }

    [Required, Display(Name = "Role")]
    public string Role { get; set; }  // "Admin" or "User"
}
```

**Important Notes**:
- `UserId` is the primary key and stores the user's email address
- `ConfirmPassword` is not mapped to the database
- Data annotations provide both server-side and client-side validation

### Data Layer

#### UserContext
**File**: `Data/UserContext.cs`

```csharp
public class UserContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Role).IsRequired();
        });
    }
}
```

**Connection Configuration** (from `Program.cs`):
- **Retry Policy**: 10 retries, 60s max delay
- **Error numbers**: 2, 53, 121, 232, 258, 1205 (connection/deadlock errors)
- **Command Timeout**: 600 seconds
- **Connection Pool**: Max 200 connections
- **SSL/TLS**: TrustServerCertificate=true

**Connection String** (from `appsettings.json`):
```
Server=PIO-LAP-624;
Database=db_MigratedLginMVC_13_1;
user id=sa;
password=Test@123;
TrustServerCertificate=true;
MultipleActiveResultSets=true;
Connect Timeout=120;
Pooling=true;
Max Pool Size=200
```

### Services

#### PasswordHashService
**File**: `Services/PasswordHashService.cs`

**Interface**: `IPasswordHashService`

**Method**:
```csharp
string HashPassword(string password)
```

**Implementation**: SHA1 hashing (legacy compatibility)
```csharp
// Converts password to ASCII bytes → SHA1 hash → Concatenated byte values as string
// Example: "Admin@123" → SHA1 → "1831141042...245"
```

**Lifetime**: Scoped

**Security Note**: SHA1 is cryptographically weak, preserved for database compatibility with migrated data. For new applications, use ASP.NET Core Identity with PBKDF2/bcrypt/Argon2.

### Views

#### Structure
```
Views/
├── Shared/
│   ├── _Layout.cshtml       # Master layout with Bootstrap navbar
│   └── Error.cshtml         # Error page
├── Users/
│   ├── Login.cshtml         # Login form
│   ├── Register.cshtml      # Registration with role selection
│   └── Index.cshtml         # User list dashboard
├── Home/                     # Standard MVC home views
├── _ViewImports.cshtml      # Global using statements
└── _ViewStart.cshtml        # Default layout specification
```

**_Layout.cshtml Features**:
- Bootstrap 4.5.2 dark navbar
- Conditional Logout link (based on session)
- jQuery validation scripts
- Footer with copyright

**Form Characteristics**:
- Anti-forgery token protection (`@Html.AntiForgeryToken()`)
- jQuery validation (client-side)
- Model binding with data annotations
- Bootstrap 4 styling

### Migrations

**Migration History**:
1. `20201007183518_InitialCreate.cs` - Original EF6 migration (converted)
2. `20201007183813_relatin.cs` - Original EF6 migration (converted)
3. `20250909171418_CreateUsersTable.cs` - New EF Core migration

**Current Schema**:
```sql
CREATE TABLE Users (
    UserId NVARCHAR(128) PRIMARY KEY,
    Username NVARCHAR(MAX) NOT NULL,
    Password NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(MAX) NOT NULL
)
```

---

## Data Architecture

### Database
**Engine**: SQL Server

### ORM
**Framework**: Entity Framework Core 8.0.10

### Pattern
Code-First with Migrations

### DbContext Configuration
- **Single DbContext**: `UserContext`
- **Single DbSet**: `Users`
- **No repository layer**: Controllers directly access DbContext
- **Async operations**: All database operations use async/await

### State Management
1. **Authentication State**: Cookie-based (primary)
2. **Session State**: Redundant storage for UserId, Username, Role
3. **Authorization**: Claims-based (ClaimTypes.Role)

### Data Flow
```
User Input (View)
    ↓
Controller Action
    ↓
DbContext (Direct Access)
    ↓
EF Core (SQL Generation)
    ↓
SQL Server
```

### Caching
**Status**: No caching implemented. Direct database access on every request.

### Transaction Management
Implicit via EF Core's `SaveChangesAsync()` method.

---

## Build & Deployment

### Build System
**.NET SDK** (dotnet CLI)

### Project Type
`Microsoft.NET.Sdk.Web`

### Build Configuration
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
</PropertyGroup>
```

### Key Build Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Watch mode (hot reload)
dotnet watch run

# Publish
dotnet publish --configuration Release --output ./publish

# Test
dotnet test

# Migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Configuration Files
- **appsettings.json** - Production configuration
- **appsettings.Development.json** - Development overrides
- **launchSettings.json** - Development server settings

### Static File Serving
- **Location**: `wwwroot/`
- **CSS**: `wwwroot/css/` (Bootstrap, custom Site.css)
- **JavaScript**: `wwwroot/js/` (jQuery, validation, modernizr)

### Deployment Targets
- Self-contained or framework-dependent deployment
- IIS compatible (web.config auto-generated)
- Docker-ready (no Dockerfile provided)
- Azure App Service compatible

---

## Dependencies

### NuGet Packages (Main Project)

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.Cookies" Version="2.2.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="8.0.10" />
<PackageReference Include="System.Security.Cryptography.Algorithms" Version="4.3.1" />
```

### NuGet Packages (Test Projects)

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="NUnit" Version="4.0.1" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
<PackageReference Include="NUnit.Analyzers" Version="3.9.0" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.10" />
```

### Frontend Dependencies

**Location**: `wwwroot/js/` and `wwwroot/lib/`

- Bootstrap 4.5.2 (bootstrap.min.js, bootstrap.min.css)
- jQuery 3.5.1 (jquery-3.5.1.min.js)
- jQuery Validation (jquery.validate.min.js)
- jQuery Validation Unobtrusive (jquery.validate.unobtrusive.min.js)
- Modernizr 2.8.3 (modernizr-2.8.3.js)

**Note**: No package manager (npm/yarn). Static files bundled directly.

---

## Testing Architecture

### Test Framework
**NUnit 4.0.1**

### Test Project Structure

```
tests/
├── LoginandRegisterMVC.UnitTests/
│   ├── Controllers/
│   │   ├── UsersControllerTests.cs
│   │   └── HomeControllerTests.cs
│   └── Services/
│       └── (Password service tests can be added)
└── LoginandRegisterMVC.IntegrationTests/
    └── Controllers/
        ├── UsersControllerIntegrationTests.cs
        └── HomeControllerIntegrationTests.cs
```

### Unit Test Pattern

**Mocking**: Moq for `IPasswordHashService`

**In-Memory Database**: EF Core InMemory provider

**Setup**: Fresh database per test (Guid.NewGuid() database name)

**Controller Context**: Mock HttpContext for session access

**Example**:
```csharp
[SetUp]
public void Setup()
{
    var options = new DbContextOptionsBuilder<UserContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    _context = new UserContext(options);
    _mockPasswordHashService = new Mock<IPasswordHashService>();
    _controller = new UsersController(_context, _mockPasswordHashService.Object);
}
```

### Integration Test Pattern

**WebApplicationFactory**: Test server

**In-Memory Database**: Replaces SQL Server

**HttpClient**: Make actual HTTP requests

**Database Seeding**: Automatic via EnsureCreated()

**Example**:
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with in-memory database
            services.AddDbContext<UserContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb");
            });
        });
    }
}
```

### Test Execution

```bash
# All tests
dotnet test

# Unit tests only
dotnet test --filter "FullyQualifiedName~UnitTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Specific test
dotnet test --filter "FullyQualifiedName~Login_ValidCredentials"

# With coverage
dotnet test /p:CollectCoverage=true
```

---

## Security Architecture

### Authentication Flow

```
1. User submits login form
   ↓
2. Password hashed with SHA1
   ↓
3. Database query for UserId + hashed password
   ↓
4. Create claims (Name, NameIdentifier, Username, Role)
   ↓
5. Sign in with Cookie Authentication
   ↓
6. Store session data (UserId, Username, Role)
   ↓
7. Redirect to Index
```

### Authorization

- **Attribute-based**: `[Authorize]` on protected actions
- **Role-based**: Claims with `ClaimTypes.Role`
- **Session-based**: Redundant check in views (legacy compatibility)

### Security Measures Implemented

✅ Cookie authentication with HttpOnly flag
✅ Anti-forgery tokens (`@Html.AntiForgeryToken()`)
✅ EF Core parameterized queries (SQL injection protection)
✅ HSTS in production
✅ HTTPS redirection

### Known Security Limitations

❌ **SHA1 password hashing** (weak, legacy compatibility)
❌ **No email verification**
❌ **No rate limiting** on login
❌ **No password complexity requirements**
❌ **No Two-Factor Authentication (2FA)**
❌ **No password reset functionality**
❌ **Credentials in appsettings.json** (should use environment variables)

### Security Best Practices Recommendations

1. **Migrate to ASP.NET Core Identity** with modern password hashing
2. **Implement rate limiting** for login attempts
3. **Add email verification** for registration
4. **Enforce password complexity** requirements
5. **Implement Two-Factor Authentication (2FA)**
6. **Use Azure Key Vault** or environment variables for secrets
7. **Add audit logging** for sensitive operations

---

## Migration Context

### Original System
- **.NET Framework 4.7.2**
- **ASP.NET MVC 5**
- **Entity Framework 6.4.4**
- **Forms Authentication**

### Migrated System
- **.NET 8**
- **ASP.NET Core MVC 8**
- **Entity Framework Core 8.0.10**
- **Cookie Authentication**

### Migration Highlights

✅ **100% UI fidelity preserved** (Bootstrap 4.5.2 exact match)
✅ **Database migration history preserved**
✅ **Legacy password hashing maintained** (SHA1)
✅ **Session management preserved** (redundancy from legacy)
✅ **Auto-seeding pattern maintained**

### Breaking Changes Handled

- Forms Authentication → Cookie Authentication
- Web.config → appsettings.json
- Global.asax → Program.cs middleware
- Entity Framework 6 → Entity Framework Core 8
- .NET Framework APIs → .NET Core equivalents

---

## Configuration

### Environment Variables
`ASPNETCORE_ENVIRONMENT` (Development/Production)

### Application Settings

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PIO-LAP-624;Database=db_MigratedLginMVC_13_1;..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Authentication Configuration

```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Users/Login";
        options.LogoutPath = "/Users/Logout";
        options.AccessDeniedPath = "/Users/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });
```

### Session Configuration

```csharp
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

### Default Credentials

**Auto-Seeded Admin Account**:
- Email: `admin@demo.com`
- Password: `Admin@123`
- Role: `Admin`
- Created on: First `/Users/Login` GET request

---

## Analytics & Logging

### Current Status

**NO ANALYTICS IMPLEMENTED**

The application has no analytics or tracking system in place.

### What's NOT Being Tracked

❌ User interactions
❌ Page views
❌ Button clicks
❌ Form submissions (beyond standard MVC)
❌ Custom events
❌ Performance metrics
❌ Business metrics
❌ Error tracking (beyond standard exception handling)

### Logging Infrastructure

**ASP.NET Core Built-in Logging Only**

**Purpose**: Basic framework logging for development debugging and EF Core SQL command logging.

**Configuration** (appsettings.json):
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

### Session Tracking (Not Analytics)

Session data stored for **authentication purposes only**:

**Location**: `Controllers/UsersController.cs:112-114`
```csharp
HttpContext.Session.SetString("UserId", authenticatedUser.UserId);
HttpContext.Session.SetString("Username", authenticatedUser.Username);
HttpContext.Session.SetString("Role", authenticatedUser.Role);
```

### Future Analytics Recommendations

For detailed analytics implementation guidance, see `Claude.md` section "Analytics Implementation".

**Recommended Solutions**:
1. **Application Insights** (Azure) - Best for .NET applications
2. **Google Analytics 4** - Web analytics
3. **Serilog** - Structured logging

**Integration Points**:
- `Views/Shared/_Layout.cshtml` - Add scripts
- `Program.cs` - Register analytics services
- Controllers - Add event tracking

---

## Important File Paths Reference

### Main Application Files

| Component | File Path |
|-----------|-----------|
| Entry Point | `UserManagement/src/LoginandRegisterMVC/Program.cs` |
| Users Controller | `UserManagement/src/LoginandRegisterMVC/Controllers/UsersController.cs` |
| Home Controller | `UserManagement/src/LoginandRegisterMVC/Controllers/HomeController.cs` |
| DbContext | `UserManagement/src/LoginandRegisterMVC/Data/UserContext.cs` |
| User Model | `UserManagement/src/LoginandRegisterMVC/Models/User.cs` |
| Password Service | `UserManagement/src/LoginandRegisterMVC/Services/PasswordHashService.cs` |
| Layout View | `UserManagement/src/LoginandRegisterMVC/Views/Shared/_Layout.cshtml` |
| Configuration | `UserManagement/src/LoginandRegisterMVC/appsettings.json` |

### Documentation Files

| Document | File Path |
|----------|-----------|
| Quick Reference | `Documents/Claude.md` |
| Architecture (this file) | `Documents/architecture.md` |
| Project README | `UserManagement/README.md` |
| Detailed Guide | `UserManagement/CLAUDE.md` |

### Test Files

| Test Type | File Path |
|-----------|-----------|
| Unit Tests (Users) | `UserManagement/tests/LoginandRegisterMVC.UnitTests/Controllers/UsersControllerTests.cs` |
| Integration Tests (Users) | `UserManagement/tests/LoginandRegisterMVC.IntegrationTests/Controllers/UsersControllerIntegrationTests.cs` |

---

## Summary

This application is a **modernized ASP.NET Core MVC 8 user management system** following a **simplified monolithic architecture** with:

- **Direct data access** (no repository pattern)
- **Cookie-based authentication** with claims
- **Entity Framework Core 8** with SQL Server
- **Comprehensive test coverage** (NUnit, Moq)
- **100% UI fidelity** with legacy system (Bootstrap 4.5.2)
- **C# 12 features** (primary constructors, async/await)

The codebase maintains backward compatibility with legacy password hashing while adopting modern .NET 8 patterns and practices. It is well-documented, thoroughly tested, and ready for future enhancements.

---

**For quick reference and coding guidelines, see**: `Claude.md`
**For detailed development workflows, see**: `UserManagement/CLAUDE.md`
