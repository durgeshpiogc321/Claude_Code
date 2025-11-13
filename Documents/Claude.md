# Claude Code Reference Guide

This document serves as the most up-to-date reference for Claude Code when working on this project. It contains essential information about the application architecture, conventions, and implementation details.

---

## Project Overview

This is a User Management application built with ASP.NET Core MVC 8, successfully migrated from .NET Framework 4.7.2. It provides user registration, authentication, and basic user management functionality.

**Key Technologies**: .NET 8, ASP.NET Core MVC, Entity Framework Core 8, SQL Server, Bootstrap 4.5.2

---

## Architecture Summary

For complete architectural details, see `architecture.md`. Key points:

- **Pattern**: Monolithic MVC (no repository pattern, controllers directly access DbContext)
- **Authentication**: Cookie-based authentication with claims
- **Database**: SQL Server with Entity Framework Core
- **Testing**: NUnit with unit and integration tests

### Project Structure
```
UserManagement/
├── src/LoginandRegisterMVC/        # Main application
│   ├── Controllers/                # MVC controllers
│   ├── Models/                     # Entity models
│   ├── Data/                       # DbContext
│   ├── Services/                   # Business services
│   ├── Views/                      # Razor views
│   └── wwwroot/                    # Static files
└── tests/                          # Test projects
    ├── LoginandRegisterMVC.UnitTests/
    └── LoginandRegisterMVC.IntegrationTests/
```

---

## Key Components Reference

### Controllers

#### UsersController (`Controllers/UsersController.cs`)
Primary controller for user management and authentication.

**Dependencies**: `UserContext`, `IPasswordHashService`

**Key Methods**:
- `Register()` - GET/POST for user registration
- `Login()` - GET/POST for user authentication
- `Index()` - Lists all users (requires [Authorize])
- `Logout()` - Signs out user

**Important Notes**:
- Auto-seeds admin user (admin@demo.com / Admin@123) on first login page access
- Both Password and ConfirmPassword are hashed before saving (legacy behavior)
- Uses both cookie authentication AND session storage (redundancy from legacy migration)

#### HomeController (`Controllers/HomeController.cs`)
Basic informational pages: Index, About, Contact, Error

### Data Layer

#### UserContext (`Data/UserContext.cs`)
Entity Framework Core DbContext with single `Users` DbSet.

**Configuration**:
- Connection string: `DefaultConnection` in appsettings.json
- Retry policy: 10 retries, 60s max delay
- Command timeout: 600s
- Connection pooling: max 200 connections

#### User Model (`Models/User.cs`)
```csharp
public class User
{
    [Key] public string UserId { get; set; }      // Email, Primary Key
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
    [NotMapped] public string ConfirmPassword { get; set; }
    [Required] public string Role { get; set; }
}
```

### Services

#### PasswordHashService (`Services/PasswordHashService.cs`)
**Interface**: `IPasswordHashService`

**Method**: `HashPassword(string password)` returns SHA1 hash as concatenated byte string

**IMPORTANT**: Uses SHA1 for backward compatibility with migrated database. NOT recommended for new applications. Consider upgrading to ASP.NET Core Identity with PBKDF2/bcrypt/Argon2 for new projects.

---

## Development Guidelines

### Code Style
- Use C# 12 features (primary constructors, nullable reference types)
- Use async/await for all database operations
- Follow existing naming conventions
- Add XML documentation for public methods

### Database Changes
1. Make model changes
2. Create migration: `dotnet ef migrations add <MigrationName>`
3. Review generated migration
4. Apply migration: `dotnet ef database update`
5. Test thoroughly

### Testing Requirements
- Write unit tests for new controller actions
- Write integration tests for user-facing workflows
- Use in-memory database for tests
- Mock dependencies with Moq
- Maintain test coverage

**Run Tests**:
```bash
cd UserManagement
dotnet test
```

### Adding New Features

When adding new features:
1. **Controllers**: Add new actions or controllers in `Controllers/`
2. **Models**: Add new entities in `Models/`, update `UserContext.cs`
3. **Services**: Add new services in `Services/`, register in `Program.cs`
4. **Views**: Add new Razor views in `Views/`
5. **Routes**: Follow existing convention: `/{controller}/{action}/{id?}`
6. **Authorization**: Use `[Authorize]` attribute for protected actions
7. **Tests**: Add corresponding unit and integration tests

---

## .NET Core Coding Standards

### Documentation Reference Requirement

**CRITICAL**: All code development MUST reference and follow documentation in:
- **`/Documents/architecture.md`** - Complete architectural reference
- **`/Documents/Claude.md`** - This quick reference guide
- **`/Planning/`** - Project plans, design decisions, and specifications

Before writing any code:
1. **Review** existing architecture documentation
2. **Consult** planning documents for design decisions
3. **Follow** established patterns and conventions
4. **Update** documentation when making architectural changes

### SOLID Principles

#### Single Responsibility Principle (SRP)
- Each class should have ONE reason to change
- Controllers handle HTTP concerns only
- Services handle business logic
- Repositories handle data access (if implementing repository pattern)

**Example**:
```csharp
// GOOD: Single responsibility
public class PasswordHashService : IPasswordHashService
{
    public string HashPassword(string password) => /* hashing logic */;
}

// BAD: Multiple responsibilities
public class UserService
{
    public void CreateUser() { /* db access, validation, email sending */ }
}
```

#### Open/Closed Principle (OCP)
- Open for extension, closed for modification
- Use interfaces and abstract classes
- Extend behavior through inheritance or composition

**Example**:
```csharp
// GOOD: Extensible via interface
public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}

public class EmailNotificationService : INotificationService { }
public class SmsNotificationService : INotificationService { }

// BAD: Modifying existing code for new notification types
public class NotificationService
{
    public void Send(string type)
    {
        if (type == "email") { /* email logic */ }
        // Need to modify this method to add SMS
    }
}
```

#### Liskov Substitution Principle (LSP)
- Derived classes must be substitutable for their base classes
- Override methods should honor base class contracts

**Example**:
```csharp
// GOOD: Derived class honors contract
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id);
}

public class UserRepository : IRepository<User>
{
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }
}

// BAD: Violating contract
public class CachedUserRepository : IRepository<User>
{
    public async Task<User?> GetByIdAsync(int id)
    {
        throw new NotImplementedException(); // Violates LSP
    }
}
```

#### Interface Segregation Principle (ISP)
- Many client-specific interfaces are better than one general-purpose interface
- Don't force clients to depend on interfaces they don't use

**Example**:
```csharp
// GOOD: Segregated interfaces
public interface IReadUserService
{
    Task<User?> GetByIdAsync(string id);
    Task<List<User>> GetAllAsync();
}

public interface IWriteUserService
{
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
}

// BAD: Fat interface
public interface IUserService
{
    Task<User?> GetByIdAsync(string id);
    Task<List<User>> GetAllAsync();
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task ExportToCsv(); // Not all clients need this
    Task ImportFromCsv(); // Not all clients need this
}
```

#### Dependency Inversion Principle (DIP)
- Depend on abstractions, not concretions
- High-level modules should not depend on low-level modules

**Example**:
```csharp
// GOOD: Depend on abstraction
public class UsersController(IUserRepository userRepository,
                             IPasswordHashService passwordHashService) : Controller
{
    // Dependencies are abstractions
}

// BAD: Depend on concrete implementation
public class UsersController : Controller
{
    private readonly SqlUserRepository _userRepository = new SqlUserRepository();
    // Tightly coupled to concrete implementation
}
```

### Code Quality Standards

#### Naming Conventions

**Classes and Interfaces**:
```csharp
// PascalCase for classes, interfaces, public members
public class UserController { }
public interface IPasswordHashService { }
public class PasswordHashService : IPasswordHashService { }
```

**Methods and Properties**:
```csharp
// PascalCase for public methods and properties
public async Task<IActionResult> RegisterUser(User user) { }
public string Username { get; set; }
```

**Private Fields**:
```csharp
// _camelCase for private fields
private readonly UserContext _context;
private readonly IPasswordHashService _passwordHashService;
```

**Local Variables and Parameters**:
```csharp
// camelCase for local variables and parameters
public async Task<User?> GetUserById(string userId)
{
    var authenticatedUser = await _context.Users.FindAsync(userId);
    return authenticatedUser;
}
```

**Constants**:
```csharp
// PascalCase for constants
public const int MaxLoginAttempts = 5;
public const string DefaultRole = "User";
```

#### Code Organization

**File Structure**:
```
- One class per file
- File name matches class name
- Organize using statements (System first, then third-party, then project)
```

**Method Ordering**:
```csharp
public class UsersController : Controller
{
    // 1. Private fields
    private readonly UserContext _context;

    // 2. Constructor
    public UsersController(UserContext context) => _context = context;

    // 3. Public methods (grouped by functionality)
    [HttpGet]
    public IActionResult Register() { }

    [HttpPost]
    public async Task<IActionResult> Register(User user) { }

    // 4. Private methods
    private async Task<bool> ValidateUserAsync(User user) { }
}
```

#### Async/Await Best Practices

**Always use async/await for I/O operations**:
```csharp
// GOOD: Async all the way
public async Task<IActionResult> Login(User user)
{
    var authenticatedUser = await _context.Users
        .FirstOrDefaultAsync(u => u.UserId == user.UserId);

    if (authenticatedUser != null)
    {
        await HttpContext.SignInAsync(/* ... */);
    }

    return View();
}

// BAD: Blocking async code
public IActionResult Login(User user)
{
    var authenticatedUser = _context.Users
        .FirstOrDefaultAsync(u => u.UserId == user.UserId)
        .Result; // NEVER use .Result or .Wait()

    return View();
}
```

**Use ConfigureAwait(false) in library code** (not needed in ASP.NET Core controllers):
```csharp
// Library/Service code
public async Task<string> ProcessDataAsync()
{
    var result = await GetDataAsync().ConfigureAwait(false);
    return result;
}
```

#### Null Safety

**Enable nullable reference types** (already enabled in this project):
```csharp
// Use nullable annotations
public async Task<User?> GetUserByIdAsync(string userId)
{
    return await _context.Users.FindAsync(userId);
}

// Check for null
if (authenticatedUser is null)
{
    return View();
}

// Or use null-coalescing
var username = authenticatedUser?.Username ?? "Guest";
```

#### Error Handling

**Use try-catch appropriately**:
```csharp
// GOOD: Catch specific exceptions
public async Task<IActionResult> Register(User user)
{
    try
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Login));
    }
    catch (DbUpdateException ex)
    {
        // Log specific error
        _logger.LogError(ex, "Failed to register user {UserId}", user.UserId);
        ModelState.AddModelError("", "Failed to register user. Please try again.");
        return View(user);
    }
}

// BAD: Catching all exceptions without handling
try
{
    // code
}
catch (Exception)
{
    // Silent failure
}
```

**Don't swallow exceptions**:
```csharp
// GOOD: Log and handle
catch (Exception ex)
{
    _logger.LogError(ex, "An error occurred");
    return StatusCode(500, "Internal server error");
}

// BAD: Empty catch block
catch (Exception)
{
    // Silent failure - never do this
}
```

### Testing Standards

#### Unit Testing

**Follow AAA Pattern** (Arrange, Act, Assert):
```csharp
[Test]
public async Task Login_ValidCredentials_ReturnsRedirectToIndex()
{
    // Arrange
    var user = new User
    {
        UserId = "test@example.com",
        Password = "hashedPassword",
        Role = "User"
    };
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();

    // Act
    var result = await _controller.Login(user);

    // Assert
    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
    var redirectResult = (RedirectToActionResult)result;
    Assert.That(redirectResult.ActionName, Is.EqualTo(nameof(UsersController.Index)));
}
```

**Test Naming Convention**:
```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Test]
public async Task Register_ValidUser_CreatesUserSuccessfully() { }

[Test]
public async Task Register_DuplicateEmail_ReturnsViewWithError() { }

[Test]
public async Task Login_InvalidPassword_ReturnsViewWithError() { }
```

**Test Independence**:
```csharp
// GOOD: Each test is independent
[SetUp]
public void Setup()
{
    var options = new DbContextOptionsBuilder<UserContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    _context = new UserContext(options);
    _controller = new UsersController(_context, _mockPasswordHashService.Object);
}

[TearDown]
public void TearDown()
{
    _context.Database.EnsureDeleted();
    _context.Dispose();
}
```

**Code Coverage Goals**:
- **Controllers**: Minimum 80% coverage
- **Services**: Minimum 90% coverage
- **Critical paths** (authentication, data modification): 100% coverage

#### Integration Testing

**Test complete workflows**:
```csharp
[Test]
public async Task UserRegistrationAndLogin_FullWorkflow_Succeeds()
{
    // Register user
    var registerResponse = await _client.PostAsync("/Users/Register",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserId"] = "newuser@example.com",
            ["Username"] = "NewUser",
            ["Password"] = "Password123",
            ["ConfirmPassword"] = "Password123",
            ["Role"] = "User"
        }));

    Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

    // Login with registered credentials
    var loginResponse = await _client.PostAsync("/Users/Login",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserId"] = "newuser@example.com",
            ["Password"] = "Password123"
        }));

    Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
```

### Security Standards

#### Input Validation

**Always validate user input**:
```csharp
[HttpPost]
[ValidateAntiForgeryToken] // REQUIRED on all POST actions
public async Task<IActionResult> Register(User user)
{
    // Server-side validation
    if (!ModelState.IsValid)
    {
        return View(user);
    }

    // Additional business logic validation
    if (await _context.Users.AnyAsync(u => u.UserId == user.UserId))
    {
        ModelState.AddModelError("UserId", "Email already registered");
        return View(user);
    }

    // Proceed with registration
}
```

**Use data annotations**:
```csharp
public class User
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(128, ErrorMessage = "Email must not exceed 128 characters")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}
```

#### Authentication & Authorization

**Always use [Authorize] attribute**:
```csharp
[Authorize] // Require authentication
public IActionResult Index()
{
    var users = await _context.Users.ToListAsync();
    return View(users);
}

[Authorize(Roles = "Admin")] // Require specific role
public IActionResult AdminDashboard()
{
    return View();
}

[AllowAnonymous] // Explicitly allow anonymous access
public IActionResult Login()
{
    return View();
}
```

#### SQL Injection Prevention

**Always use parameterized queries** (EF Core does this automatically):
```csharp
// GOOD: EF Core uses parameters
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.UserId == userId);

// GOOD: Explicit parameter
var users = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)
    .ToListAsync();

// BAD: String concatenation (NEVER DO THIS)
var query = $"SELECT * FROM Users WHERE UserId = '{userId}'";
var users = await _context.Users.FromSqlRaw(query).ToListAsync();
```

#### Sensitive Data Protection

**Never log sensitive information**:
```csharp
// GOOD: Log without sensitive data
_logger.LogInformation("User {UserId} attempted login", user.UserId);

// BAD: Logging password
_logger.LogInformation("Login attempt: {UserId} with password {Password}",
    user.UserId, user.Password); // NEVER DO THIS
```

**Store configuration securely**:
```csharp
// GOOD: Use User Secrets in development
// dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."

// GOOD: Use environment variables in production
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// BAD: Hardcoded secrets in code
var connectionString = "Server=localhost;Password=MyPassword123;"; // NEVER DO THIS
```

### Performance Standards

#### Database Queries

**Use async operations**:
```csharp
// GOOD: Async query
var users = await _context.Users.ToListAsync();

// BAD: Synchronous query
var users = _context.Users.ToList();
```

**Avoid N+1 queries**:
```csharp
// GOOD: Single query with Include
var users = await _context.Users
    .Include(u => u.Orders)
    .ToListAsync();

// BAD: N+1 queries
var users = await _context.Users.ToListAsync();
foreach (var user in users)
{
    var orders = await _context.Orders
        .Where(o => o.UserId == user.UserId)
        .ToListAsync(); // Executes N additional queries
}
```

**Use pagination for large datasets**:
```csharp
public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
{
    var users = await _context.Users
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return View(users);
}
```

**Project only needed columns**:
```csharp
// GOOD: Project to DTO
var userDtos = await _context.Users
    .Select(u => new UserDto
    {
        UserId = u.UserId,
        Username = u.Username
    })
    .ToListAsync();

// BAD: Loading entire entity when only a few fields are needed
var users = await _context.Users.ToListAsync();
var usernames = users.Select(u => u.Username);
```

#### Caching Strategies

**Implement caching for frequently accessed data**:
```csharp
public class UserService
{
    private readonly IMemoryCache _cache;
    private readonly UserContext _context;

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        var cacheKey = $"user_{userId}";

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                _cache.Set(cacheKey, user, TimeSpan.FromMinutes(10));
            }
        }

        return user;
    }
}
```

#### Response Time Goals

- **Page Load**: < 2 seconds
- **API Endpoints**: < 500ms
- **Database Queries**: < 100ms for simple queries

### Documentation Standards

#### XML Documentation

**Document all public APIs**:
```csharp
/// <summary>
/// Authenticates a user with the provided credentials.
/// </summary>
/// <param name="user">The user credentials containing UserId (email) and Password.</param>
/// <returns>
/// Returns a redirect to Index on success, or returns the login view with validation errors on failure.
/// </returns>
/// <remarks>
/// This method validates credentials by comparing hashed passwords.
/// On successful authentication, creates claims-based identity and signs in the user.
/// Also stores user information in session for backward compatibility.
/// </remarks>
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(User user)
{
    // Implementation
}
```

#### Code Comments

**Write meaningful comments**:
```csharp
// GOOD: Explains WHY, not WHAT
// Hash both Password and ConfirmPassword to maintain database compatibility
// with legacy system that stored both fields hashed
user.Password = _passwordHashService.HashPassword(user.Password);
user.ConfirmPassword = _passwordHashService.HashPassword(user.ConfirmPassword);

// BAD: States the obvious
// Set the password
user.Password = _passwordHashService.HashPassword(user.Password);
```

**Document complex logic**:
```csharp
// Configure retry policy for transient SQL Server errors
// Error codes: 2 (connection timeout), 53 (server not found),
// 121 (semaphore timeout), 232 (client unable to establish connection),
// 258 (wait operation timed out), 1205 (deadlock victim)
services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: new[] { 2, 53, 121, 232, 258, 1205 }
        )
    )
);
```

#### README and Documentation Files

**Keep documentation up-to-date**:
- Update `architecture.md` when making architectural changes
- Update `Claude.md` when adding new patterns or conventions
- Document design decisions in `/Planning/` directory
- Create ADRs (Architecture Decision Records) for significant decisions

### Code Review Checklist

#### Functionality
- ✅ Code meets all requirements
- ✅ Edge cases are handled
- ✅ Error handling is appropriate
- ✅ No hardcoded values (use configuration)

#### Architecture & Design
- ✅ Follows SOLID principles
- ✅ Follows existing patterns in codebase
- ✅ No unnecessary abstractions
- ✅ Proper separation of concerns
- ✅ References `/Documents/architecture.md` for architectural decisions

#### Code Quality
- ✅ Naming conventions followed
- ✅ Code is readable and maintainable
- ✅ No code duplication (DRY principle)
- ✅ Methods are focused and concise (< 50 lines)
- ✅ Complexity is managed (cyclomatic complexity < 10)

#### Testing
- ✅ Unit tests written and passing
- ✅ Integration tests for workflows
- ✅ Code coverage meets standards (80%+)
- ✅ Tests are independent and repeatable
- ✅ Tests follow naming conventions

#### Security
- ✅ Input validation implemented
- ✅ [Authorize] attribute used where needed
- ✅ [ValidateAntiForgeryToken] on all POST actions
- ✅ No sensitive data in logs
- ✅ No SQL injection vulnerabilities
- ✅ No XSS vulnerabilities
- ✅ Secrets stored securely (not hardcoded)

#### Performance
- ✅ Async/await used for I/O operations
- ✅ No N+1 query problems
- ✅ Pagination for large datasets
- ✅ Appropriate caching implemented
- ✅ Database queries are optimized

#### Documentation
- ✅ XML documentation for public APIs
- ✅ Complex logic is commented
- ✅ Architecture documentation updated if needed
- ✅ Planning documents consulted and updated
- ✅ README updated if public interface changed

#### Database
- ✅ Migrations created and tested
- ✅ Migration is reversible
- ✅ No data loss in migration
- ✅ Indexes added for queried columns

#### Dependencies
- ✅ No unnecessary dependencies added
- ✅ Dependencies registered in DI container
- ✅ Dependency versions are compatible

#### Deployment
- ✅ Configuration for all environments
- ✅ No environment-specific code
- ✅ Build succeeds without warnings
- ✅ All tests pass

### Additional Best Practices

#### Dependency Injection

**Register services with appropriate lifetime**:
```csharp
// Transient: New instance every time
builder.Services.AddTransient<IEmailService, EmailService>();

// Scoped: One instance per HTTP request
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

// Singleton: One instance for application lifetime
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
```

#### Configuration Management

**Use strongly-typed configuration**:
```csharp
// appsettings.json
{
  "AppSettings": {
    "MaxLoginAttempts": 5,
    "SessionTimeout": 60
  }
}

// Configuration class
public class AppSettings
{
    public int MaxLoginAttempts { get; set; }
    public int SessionTimeout { get; set; }
}

// Program.cs
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Usage in controller
public class UsersController(IOptions<AppSettings> appSettings) : Controller
{
    private readonly AppSettings _appSettings = appSettings.Value;
}
```

#### Logging Best Practices

**Use structured logging**:
```csharp
// GOOD: Structured logging
_logger.LogInformation("User {UserId} logged in successfully with role {Role}",
    user.UserId, user.Role);

// BAD: String concatenation
_logger.LogInformation($"User {user.UserId} logged in successfully");
```

**Use appropriate log levels**:
```csharp
_logger.LogTrace("Entering method GetUserById with id: {UserId}", userId);
_logger.LogDebug("Retrieved user: {User}", user);
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogWarning("Failed login attempt for user {UserId}", userId);
_logger.LogError(ex, "Failed to create user {UserId}", userId);
_logger.LogCritical(ex, "Database connection failed");
```

#### Resource Management

**Use using statements or declarations**:
```csharp
// GOOD: Using declaration (C# 8+)
public async Task ProcessFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    // stream is disposed at end of method
}

// GOOD: Using statement
public async Task ProcessFileAsync(string path)
{
    using (var stream = File.OpenRead(path))
    {
        // stream is disposed at end of block
    }
}
```

#### Exception Messages

**Provide helpful error messages**:
```csharp
// GOOD: Descriptive error message
throw new ArgumentException(
    $"User with ID '{userId}' not found in the system.",
    nameof(userId));

// BAD: Generic error message
throw new Exception("Error");
```

### Pre-Commit Checklist

Before committing code, ensure:

1. ✅ **Documentation reviewed**: Consulted `/Documents/architecture.md` and `/Documents/Claude.md`
2. ✅ **Planning reviewed**: Checked `/Planning/` for relevant design decisions
3. ✅ **Build succeeds**: `dotnet build` completes without errors or warnings
4. ✅ **Tests pass**: `dotnet test` shows all tests passing
5. ✅ **Code formatted**: Code follows project formatting standards
6. ✅ **No commented code**: Removed unnecessary commented-out code
7. ✅ **No debug code**: Removed console.WriteLine, debugger statements
8. ✅ **Git diff reviewed**: Reviewed all changes before committing
9. ✅ **Commit message**: Clear, descriptive commit message following conventions

### Continuous Improvement

- **Regular refactoring**: Improve code quality continuously
- **Update dependencies**: Keep packages up-to-date for security
- **Performance monitoring**: Profile and optimize bottlenecks
- **Security audits**: Regular security reviews
- **Documentation maintenance**: Keep all documentation current

---

## Official Microsoft .NET Best Practices

### Official Documentation References

**CRITICAL**: All .NET Core development must align with official Microsoft documentation:

📚 **Primary Resources**:
- **Microsoft Learn - .NET**: https://learn.microsoft.com/en-us/dotnet/
- **ASP.NET Core Documentation**: https://learn.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **C# Programming Guide**: https://learn.microsoft.com/en-us/dotnet/csharp/

### Official .NET Naming Conventions

**Source**: Microsoft Learn - C# Coding Conventions

#### Identifier Naming Rules (Microsoft Standard)

**PascalCase** - Use for:
- Class names: `CustomerAccount`, `OrderProcessor`
- Interface names: `IPasswordHashService`, `IUserRepository`
- Public properties: `Username`, `CustomerId`, `OrderDate`
- Public methods: `ProcessOrder()`, `CalculateTotal()`, `ValidateInput()`
- Namespace names: `MyCompany.ProjectName.Module`
- Enum types and values: `OrderStatus.Pending`, `UserRole.Admin`

**camelCase** - Use for:
- Method parameters: `userId`, `orderAmount`, `customerName`
- Local variables: `currentTotal`, `isValid`, `tempResults`

**_camelCase** - Use for:
- Private fields: `_context`, `_passwordHashService`, `_logger`

**Constants** - Use PascalCase:
```csharp
public const int MaxLoginAttempts = 5;
public const string DefaultRole = "User";
private const int DefaultTimeout = 30;
```

#### Microsoft-Recommended Code Style

**Expression-Bodied Members** (when appropriate):
```csharp
// Microsoft-recommended for simple implementations
public string FullName => $"{FirstName} {LastName}";
public void LogMessage(string message) => _logger.LogInformation(message);
```

**File-Scoped Namespaces** (C# 10+):
```csharp
// Microsoft-recommended modern syntax
namespace MyCompany.UserManagement;

public class UserService
{
    // Class implementation
}
```

**Using Declarations** (C# 8+):
```csharp
// Microsoft-recommended resource management
public async Task ProcessFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    using var reader = new StreamReader(stream);
    // Automatic disposal at method end
}
```

### Official ASP.NET Core Security Guidelines

**Source**: Microsoft ASP.NET Core Security Documentation

#### Mandatory Security Practices

**1. Anti-Forgery Token Validation**

Microsoft mandates `[ValidateAntiForgeryToken]` or `[AutoValidateAntiforgeryToken]` on all state-changing operations:

```csharp
// Microsoft-recommended: Apply globally
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
```

```csharp
// Or apply per-controller
[AutoValidateAntiforgeryToken]
public class HomeController : Controller
{
    [HttpPost]
    public IActionResult Create(User user)
    {
        // Automatically validated for POST
    }
}
```

**2. Authentication & Authorization**

Microsoft-recommended authentication setup:

```csharp
// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
    });

// Always call in correct order
app.UseAuthentication();
app.UseAuthorization();
```

**Secure endpoints with [Authorize]**:
```csharp
[Authorize] // Requires authentication
public class AdminController : Controller { }

[Authorize(Roles = "Admin")] // Requires specific role
public IActionResult AdminDashboard() { }

[AllowAnonymous] // Explicitly allow public access
public IActionResult PublicInfo() { }
```

**3. Input Validation**

Microsoft-recommended validation approach:

```csharp
public class User
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(128, ErrorMessage = "Email must not exceed 128 characters")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}
```

**4. Prevent Over-Posting Attacks**

Microsoft recommends using DTOs or specific binding:

```csharp
// GOOD: Use DTO to control what can be bound
public class UserRegistrationDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    // Excludes sensitive properties like IsAdmin
}

[HttpPost]
public IActionResult Register(UserRegistrationDto dto)
{
    // Only specified properties can be bound
}

// GOOD: Use [Bind] attribute
[HttpPost]
public IActionResult Update([Bind("Email,Username")] User user)
{
    // Only Email and Username can be updated
}
```

**5. Secure Secrets Management**

Microsoft-recommended approach:

```csharp
// Development: Use User Secrets
// dotnet user-secrets init
// dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."

// Production: Use environment variables or Azure Key Vault
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// NEVER hardcode secrets
// BAD: var connectionString = "Server=...;Password=MyPassword123;"; // NEVER DO THIS
```

### Official Entity Framework Core Performance Guidelines

**Source**: Microsoft EF Core Performance Documentation

#### Microsoft-Recommended Query Patterns

**1. Use AsNoTracking for Read-Only Queries**

```csharp
// Microsoft-recommended for read-only scenarios
var users = await context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();
```

**2. Avoid N+1 Query Problems**

```csharp
// BAD: N+1 queries (NOT Microsoft-recommended)
var blogs = await context.Blogs.ToListAsync();
foreach (var blog in blogs)
{
    // Each iteration executes a separate query
    var posts = await context.Posts
        .Where(p => p.BlogId == blog.Id)
        .ToListAsync();
}

// GOOD: Microsoft-recommended single query with Include
var blogs = await context.Blogs
    .Include(b => b.Posts)
    .ToListAsync();
```

**3. Use Projection for Minimal Data Transfer**

```csharp
// BAD: Loading entire entity
var users = await context.Users.ToListAsync();
var userNames = users.Select(u => u.Username).ToList();

// GOOD: Microsoft-recommended projection
var userNames = await context.Users
    .Select(u => u.Username)
    .ToListAsync();
```

**Generated SQL Comparison**:
```sql
-- Bad: SELECT [u].[Id], [u].[Username], [u].[Email], [u].[Password], ...
-- Good: SELECT [u].[Username] FROM [Users] AS [u]
```

**4. Use Compiled Queries for Repeated Patterns**

```csharp
// Microsoft-recommended for frequently executed queries
private static readonly Func<UserContext, string, Task<User>> GetUserByEmail =
    EF.CompileAsyncQuery((UserContext context, string email) =>
        context.Users.FirstOrDefault(u => u.UserId == email));

// Usage
var user = await GetUserByEmail(context, email);
```

**5. Pagination for Large Datasets**

```csharp
// Microsoft-recommended pagination pattern
public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
{
    var users = await context.Users
        .OrderBy(u => u.Username)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync();

    return View(users);
}
```

**6. Parameterization for Query Plan Caching**

```csharp
// GOOD: Microsoft-recommended parameterization
var userId = "user@example.com";
var user = await context.Users
    .Where(u => u.UserId == userId)
    .FirstOrDefaultAsync();

// BAD: Constants cause separate query plans
var user1 = await context.Users.Where(u => u.UserId == "user1@example.com").FirstOrDefaultAsync();
var user2 = await context.Users.Where(u => u.UserId == "user2@example.com").FirstOrDefaultAsync();
// Each query above compiles a separate plan
```

**7. Use TagWith for Query Identification**

```csharp
// Microsoft-recommended for debugging and monitoring
var users = await context.Users
    .TagWith("GetActiveUsers - Admin Dashboard")
    .Where(u => u.IsActive)
    .ToListAsync();
```

**Generated SQL**:
```sql
-- GetActiveUsers - Admin Dashboard
SELECT [u].[Id], [u].[Username], ...
FROM [Users] AS [u]
WHERE [u].[IsActive] = 1
```

### Official XML Documentation Standards

**Source**: Microsoft C# Programming Guide

#### Microsoft-Recommended Documentation Tags

**Complete Method Documentation**:
```csharp
/// <summary>
/// Authenticates a user with the provided credentials.
/// </summary>
/// <param name="user">The user credentials containing UserId (email) and Password.</param>
/// <returns>
/// Returns a <see cref="RedirectToActionResult"/> to Index on success,
/// or returns the login view with <see cref="ModelState"/> errors on failure.
/// </returns>
/// <remarks>
/// This method validates credentials by comparing hashed passwords using SHA1.
/// On successful authentication, creates claims-based identity and signs in the user.
/// <para>
/// Session data is also stored for backward compatibility with legacy system.
/// </para>
/// </remarks>
/// <example>
/// This shows how to call the Login method:
/// <code>
/// var user = new User { UserId = "admin@example.com", Password = "password123" };
/// var result = await controller.Login(user);
/// </code>
/// </example>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="user"/> is null.
/// </exception>
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(User user)
{
    // Implementation
}
```

**Standard Microsoft Tags**:

- `<summary>` - Brief description (required for all public members)
- `<param name="name">` - Parameter description
- `<returns>` - Return value description
- `<remarks>` - Additional detailed information
- `<example>` - Usage examples
- `<code>` - Code samples (multi-line)
- `<c>` - Inline code references
- `<see cref="Type"/>` - Cross-references to other types
- `<seealso cref="Type"/>` - Related references
- `<exception cref="ExceptionType">` - Exceptions thrown
- `<para>` - Paragraph breaks in remarks

### Microsoft Code Analysis & Quality Tools

#### Enable All Code Analysis Rules

**Microsoft-recommended project configuration**:

```xml
<PropertyGroup>
  <!-- Enable nullable reference types -->
  <Nullable>enable</Nullable>

  <!-- Enable all code analysis rules -->
  <AnalysisMode>All</AnalysisMode>

  <!-- Enforce code style on build -->
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

  <!-- Treat warnings as errors in production -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

#### EditorConfig for Code Style

**Microsoft-recommended .editorconfig**:

```ini
# Top-most EditorConfig file
root = true

# All files
[*]
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

# C# files
[*.cs]
indent_size = 4

# Code style rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Prefer 'var' when type is obvious
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Prefer expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion

# Require accessibility modifiers
dotnet_style_require_accessibility_modifiers = always:warning

# Prefer 'using' declarations
csharp_prefer_simple_using_statement = true:suggestion
```

### Official Microsoft Anti-Patterns to Avoid

#### 1. Don't Block Async Code

```csharp
// BAD: Blocking async code (Microsoft warns against this)
public ActionResult Index()
{
    var result = GetDataAsync().Result; // Deadlock risk!
    return View(result);
}

// GOOD: Async all the way (Microsoft-recommended)
public async Task<ActionResult> Index()
{
    var result = await GetDataAsync();
    return View(result);
}
```

#### 2. Don't Use Constants in Queries

```csharp
// BAD: Query plan cache pollution
var user1 = await context.Users.Where(u => u.UserId == "user1@example.com").FirstOrDefaultAsync();
var user2 = await context.Users.Where(u => u.UserId == "user2@example.com").FirstOrDefaultAsync();

// GOOD: Use parameters
var getUserByEmail = async (string email) =>
    await context.Users.Where(u => u.UserId == email).FirstOrDefaultAsync();
```

#### 3. Don't Swallow Exceptions

```csharp
// BAD: Silent failure
try
{
    await ProcessDataAsync();
}
catch (Exception)
{
    // Silent failure - Microsoft strongly advises against this
}

// GOOD: Log and handle appropriately
try
{
    await ProcessDataAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process data");
    throw; // or handle appropriately
}
```

#### 4. Don't Ignore Disposal

```csharp
// BAD: Manual disposal management
var stream = File.OpenRead(path);
try
{
    // Use stream
}
finally
{
    stream?.Dispose();
}

// GOOD: Microsoft-recommended using declaration
using var stream = File.OpenRead(path);
// Automatic disposal at scope end
```

### Microsoft Performance Best Practices Summary

**From Official EF Core Performance Documentation**:

1. **Always use AsNoTracking for read-only queries** - 30-40% performance improvement
2. **Use projection instead of loading entire entities** - Reduces data transfer
3. **Avoid N+1 queries with Include/ThenInclude** - Single query vs. N queries
4. **Use compiled queries for repeated patterns** - Eliminates query compilation overhead
5. **Implement pagination for large datasets** - Use Skip/Take
6. **Leverage query caching with parameterization** - Reuses query plans
7. **Use TagWith for query identification** - Debugging and monitoring
8. **Enable connection pooling** - Reuses database connections
9. **Use async/await for all I/O operations** - Improves scalability
10. **Avoid loading navigation properties unless needed** - Explicit loading when required

### Official Microsoft Security Checklist

Based on ASP.NET Core Security Documentation:

- ✅ **Use [ValidateAntiForgeryToken] on all POST actions**
- ✅ **Never trust user input - always validate**
- ✅ **Use parameterized queries (EF Core does this automatically)**
- ✅ **Store secrets in User Secrets (dev) or Azure Key Vault (prod)**
- ✅ **Enable HTTPS with HSTS in production**
- ✅ **Use [Authorize] attribute for protected resources**
- ✅ **Implement proper password hashing (ASP.NET Core Identity)**
- ✅ **Enable code analysis and treat warnings as errors**
- ✅ **Keep dependencies up-to-date for security patches**
- ✅ **Never log sensitive information (passwords, tokens)**
- ✅ **Implement rate limiting for authentication endpoints**
- ✅ **Use DTOs to prevent over-posting attacks**

### Alignment with This Project

**Current Project Status**:

✅ **Aligned with Microsoft Standards**:
- Uses C# 12 features (primary constructors, nullable reference types)
- Implements async/await throughout
- Uses cookie-based authentication (Microsoft-recommended)
- Applies [ValidateAntiForgeryToken] on POST actions
- Uses EF Core with SQL Server
- Implements proper using statements
- Uses dependency injection
- Has comprehensive test coverage

⚠️ **Areas for Improvement** (documented in Security Considerations):
- **Password Hashing**: Uses SHA1 (legacy compatibility) - Microsoft recommends ASP.NET Core Identity with PBKDF2/Argon2
- **Secrets Management**: Credentials in appsettings.json - Microsoft recommends User Secrets/Azure Key Vault
- **Code Analysis**: Not enabled - Microsoft recommends AnalysisMode=All

**When modernizing, prioritize**:
1. Migrate to ASP.NET Core Identity for password management
2. Move secrets to User Secrets (dev) and environment variables (prod)
3. Enable code analysis with AnalysisMode=All
4. Add rate limiting for login attempts
5. Implement email verification

### Reference Implementation

For Microsoft-recommended project structure and patterns, see:
- **Clean Architecture Template**: https://github.com/ardalis/CleanArchitecture
- **eShopOnWeb Reference**: https://github.com/dotnet-architecture/eShopOnWeb
- **Official ASP.NET Core Samples**: https://github.com/dotnet/AspNetCore.Docs.Samples

---

## Analytics Implementation

### Current Status: NO ANALYTICS IMPLEMENTED

The application currently has **no analytics or tracking system** in place. Only standard ASP.NET Core logging exists.

### What's NOT Being Tracked
- User interactions
- Page views
- Button clicks
- Form submissions (beyond standard MVC form handling)
- Custom events
- Performance metrics
- Business metrics
- Error tracking (beyond standard exception handling)

### Existing Logging Infrastructure

**ASP.NET Core Logging Only**

**Configuration** (`appsettings.json`):
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

**Usage**: Logging is configured but only used in integration tests, not in production code.

### Session Tracking (Not Analytics)

The application tracks user sessions for **authentication purposes only**:

**Location**: `Controllers/UsersController.cs:112-114`
```csharp
HttpContext.Session.SetString("UserId", authenticatedUser.UserId);
HttpContext.Session.SetString("Username", authenticatedUser.Username);
HttpContext.Session.SetString("Role", authenticatedUser.Role);
```

This is purely for authentication state management, not analytics.

### Future Analytics Recommendations

If analytics implementation is needed, consider:

#### 1. Integration Points
- **`Views/Shared/_Layout.cshtml`**: Add analytics scripts in `<head>` or before `</body>`
- **`Program.cs`**: Register analytics services in DI container
- **Controllers**: Add event tracking in action methods

#### 2. Recommended Analytics Solutions
- **Application Insights** (Azure) - Best for .NET applications
- **Google Analytics 4** - Web analytics
- **Custom logging with Serilog** - Structured logging

#### 3. Events to Consider Tracking
- User registration events
- Login/logout events
- Failed login attempts
- User role changes
- Page views per user session
- Form validation errors
- Application errors and exceptions

#### 4. Implementation Example

**Program.cs**:
```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Or add custom analytics service
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
```

**Controller**:
```csharp
public class UsersController(UserContext context,
                             IPasswordHashService passwordHashService,
                             IAnalyticsService analyticsService) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Login(User user)
    {
        // ... authentication logic ...

        if (authenticatedUser != null)
        {
            await analyticsService.TrackEventAsync("UserLogin", new
            {
                UserId = authenticatedUser.UserId,
                Role = authenticatedUser.Role,
                Timestamp = DateTime.UtcNow
            });

            // ... rest of logic ...
        }
    }
}
```

**_Layout.cshtml** (for Google Analytics):
```html
<head>
    <!-- Other head content -->

    <!-- Google Analytics -->
    <script async src="https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID"></script>
    <script>
        window.dataLayer = window.dataLayer || [];
        function gtag(){dataLayer.push(arguments);}
        gtag('js', new Date());
        gtag('config', 'GA_MEASUREMENT_ID');
    </script>
</head>
```

---

## Configuration Files

### appsettings.json
Production configuration including:
- Database connection string (change for production!)
- Logging levels
- Allowed hosts

### appsettings.Development.json
Development-specific overrides (if any)

### launchSettings.json
Development server configuration:
- HTTPS: `https://localhost:62406`
- HTTP: `http://localhost:62407`

---

## Security Considerations

### Current Security Measures
- Cookie authentication with HttpOnly flag
- Anti-forgery tokens on all POST forms
- Authorization attributes on protected actions
- SQL injection protection via EF Core parameterized queries
- HTTPS in production

### Known Security Limitations
1. **SHA1 Password Hashing**: Cryptographically weak, kept for database compatibility
   - **Recommendation**: Migrate to ASP.NET Core Identity with modern hashing
2. **No Email Verification**: Users can register with any email
3. **No Rate Limiting**: Vulnerable to brute force attacks
4. **No Password Complexity Requirements**: Weak passwords allowed
5. **No Two-Factor Authentication (2FA)**
6. **No Password Reset Functionality**

### Security Best Practices
- Never commit `appsettings.json` with real credentials
- Use environment variables or Azure Key Vault for production secrets
- Implement rate limiting for login attempts
- Add email verification for registration
- Implement password complexity requirements
- Add audit logging for sensitive operations

---

## Common Tasks

### Run Application
```bash
cd UserManagement/src/LoginandRegisterMVC
dotnet run
```

### Run with Hot Reload
```bash
dotnet watch run
```

### Run Tests
```bash
cd UserManagement
dotnet test
```

### Create Migration
```bash
cd UserManagement/src/LoginandRegisterMVC
dotnet ef migrations add <MigrationName>
```

### Apply Migration
```bash
dotnet ef database update
```

### Rollback Migration
```bash
dotnet ef database update <PreviousMigrationName>
```

### Build Release
```bash
dotnet build --configuration Release
```

### Publish Application
```bash
dotnet publish --configuration Release --output ./publish
```

---

## Troubleshooting

### Database Connection Issues
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database exists: `dotnet ef database update`
4. Check firewall rules for SQL Server port (1433)

### Migration Issues
1. Ensure no pending changes: `dotnet ef migrations list`
2. Remove failed migration: `dotnet ef migrations remove`
3. Rebuild solution before creating migration
4. Check for conflicting migrations

### Authentication Issues
1. Clear browser cookies
2. Check session timeout (60 minutes)
3. Verify cookie authentication is configured in `Program.cs`
4. Check that `[Authorize]` attributes are applied correctly

---

## Default Test Credentials

**Admin Account** (auto-created):
- Email: admin@demo.com
- Password: Admin@123
- Role: Admin

---

## Important Files Reference

### Entry Point
- `Program.cs` - Application configuration and startup

### Controllers
- `Controllers/UsersController.cs` - User management and authentication
- `Controllers/HomeController.cs` - Basic pages

### Data & Models
- `Data/UserContext.cs` - EF Core DbContext
- `Models/User.cs` - User entity model

### Services
- `Services/PasswordHashService.cs` - SHA1 password hashing

### Views
- `Views/Shared/_Layout.cshtml` - Master layout
- `Views/Users/Login.cshtml` - Login page
- `Views/Users/Register.cshtml` - Registration page
- `Views/Users/Index.cshtml` - User list page

### Configuration
- `appsettings.json` - Production configuration
- `appsettings.Development.json` - Development configuration
- `Properties/launchSettings.json` - Launch profiles

### Database
- `Migrations/` - EF Core migrations
- Current migration: `20250909171418_CreateUsersTable.cs`

---

## Document Information

**Last Updated**: 2025-11-13
**Version**: 2.1
**Purpose**: Quick reference for Claude Code when working on this project
**Related Documents**: `architecture.md` (detailed architecture documentation)

**Update Notes**:
- v2.1 (2025-11-13): Enhanced coding standards with official Microsoft .NET documentation
  - Added official .NET naming conventions and code style guidelines
  - Integrated ASP.NET Core security best practices from official docs
  - Enhanced EF Core performance optimization guidance
  - Added Microsoft-recommended patterns and anti-patterns
  - Included official XML documentation standards
  - Referenced official Microsoft Learn documentation
- v2.0 (2025-11-13): Added comprehensive .NET Core coding standards section
  - SOLID principles with examples
  - Code quality standards (naming, organization, async/await, null safety, error handling)
  - Testing standards (unit, integration, coverage goals)
  - Security standards (validation, authorization, SQL injection prevention)
  - Performance standards (database queries, caching, response time goals)
  - Documentation standards (XML docs, comments, ADRs)
  - Complete code review checklist
  - Additional best practices (DI, configuration, logging, resource management)
  - Pre-commit checklist
- v1.1 (2025-11-13): Analytics section verified and confirmed accurate
- v1.0 (2025-11-12): Initial version with architecture documentation created

---

## Notes for Claude Code

When working on this project:
1. **Preserve legacy behaviors** unless explicitly asked to modernize
2. **Write tests** for all new features
3. **Use async/await** for all database operations
4. **Follow existing patterns** (direct DbContext access, no repository pattern)
5. **Update this document** if you make significant architectural changes
6. **Consider security implications** for all changes
7. **Check both unit and integration tests** before finalizing changes

For detailed architectural information, always refer to `architecture.md`.
