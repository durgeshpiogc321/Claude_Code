# User Management Module - Technical Specifications

**Document Version:** 1.0
**Created:** 2025-01-13
**Status:** Approved

---

## Table of Contents

1. [Technology Stack](#technology-stack)
2. [Code Templates & Patterns](#code-templates--patterns)
3. [API Endpoints](#api-endpoints)
4. [Database Specifications](#database-specifications)
5. [Security Specifications](#security-specifications)
6. [Performance Requirements](#performance-requirements)
7. [File Upload Specifications](#file-upload-specifications)
8. [Error Handling](#error-handling)

---

## 1. Technology Stack

### Core Framework
- **Runtime:** .NET 8.0
- **Framework:** ASP.NET Core MVC 8.0.11
- **Language:** C# 12 (with primary constructors, nullable reference types)

### Data Access
- **ORM:** Entity Framework Core 8.0.11
- **Database:** SQL Server (any version supporting DATETIME2)
- **Migration Tool:** EF Core Migrations

### UI/UX
- **CSS Framework:** Bootstrap 4.5.2
- **JavaScript Library:** jQuery 3.5.1
- **Validation:** jQuery Validation 1.19.5
- **Notifications:** SweetAlert2 11.x
- **Icons:** Font Awesome 5.x (optional) or Bootstrap Icons

### Security
- **Authentication:** ASP.NET Core Cookie Authentication
- **Authorization:** Policy-based authorization
- **Password Hashing:** Microsoft.AspNetCore.Identity PasswordHasher
- **Rate Limiting:** AspNetCoreRateLimit 5.0.0
- **Validation:** FluentValidation.AspNetCore 11.3.0

### Testing
- **Unit Testing:** NUnit 4.3.1
- **Mocking:** Moq 4.20.72
- **Integration Testing:** Microsoft.AspNetCore.Mvc.Testing 8.0.11
- **Code Coverage:** Coverlet 6.0.2

### Logging & Monitoring
- **Logging:** Serilog.AspNetCore 8.0.2
- **Health Checks:** AspNetCore.HealthChecks.SqlServer 8.0.2

---

## 2. Code Templates & Patterns

### 2.1 Repository Pattern Template

```csharp
// Interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<List<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> SaveChangesAsync();
}

// Implementation
public class Repository<T>(DbContext context) : IRepository<T> where T : class
{
    protected readonly DbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }

    // ... other implementations
}
```

### 2.2 Service Layer Pattern Template

```csharp
// Interface
public interface IUserService
{
    Task<ServiceResult<UserDetailsViewModel>> GetUserByIdAsync(string userId);
    Task<ServiceResult> CreateUserAsync(CreateUserViewModel model);
    Task<ServiceResult> UpdateUserAsync(EditUserViewModel model);
    Task<ServiceResult> DeleteUserAsync(string userId);
}

// Service Result Class
public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public static ServiceResult Success() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Success(T data) => new() { Success = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
}
```

### 2.3 Controller Action Template

```csharp
[Authorize(Policy = "PolicyName")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ActionName(ViewModel model)
{
    // 1. Validation
    if (!ModelState.IsValid)
        return View(model);

    try
    {
        // 2. Call service
        var result = await _service.PerformOperationAsync(model);

        // 3. Handle result
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Operation completed successfully";
            return RedirectToAction(nameof(Index));
        }

        // 4. Handle errors
        ModelState.AddModelError("", result.ErrorMessage ?? "Operation failed");
        return View(model);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in {Action}", nameof(ActionName));
        ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
        return View(model);
    }
}
```

### 2.4 Authorization Handler Template

```csharp
// Requirement
public class CustomRequirement : IAuthorizationRequirement
{
    public string RequiredPermission { get; set; }
}

// Handler
public class CustomAuthorizationHandler : AuthorizationHandler<CustomRequirement, string>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomRequirement requirement,
        string resourceId)
    {
        var userId = context.User.FindFirst(ClaimTypes.Name)?.Value;
        var isAdmin = context.User.IsInRole("Admin");

        // Authorization logic
        if (isAdmin || userId == resourceId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Registration in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomPolicy", policy =>
        policy.Requirements.Add(new CustomRequirement { RequiredPermission = "CanEdit" }));
});
builder.Services.AddSingleton<IAuthorizationHandler, CustomAuthorizationHandler>();
```

### 2.5 FluentValidation Template

```csharp
public class ViewModelValidator : AbstractValidator<ViewModel>
{
    private readonly IService _service;

    public ViewModelValidator(IService service)
    {
        _service = service;

        RuleFor(x => x.Property)
            .NotEmpty().WithMessage("Property is required")
            .Length(3, 100).WithMessage("Property must be between 3 and 100 characters")
            .MustAsync(BeUniqueAsync).WithMessage("Property already exists");
    }

    private async Task<bool> BeUniqueAsync(string value, CancellationToken token)
    {
        return await _service.IsUniqueAsync(value);
    }
}
```

---

## 3. API Endpoints

### 3.1 User Management Endpoints

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| GET | `/Users` | Authenticated | List users (paginated, searchable, sortable) |
| GET | `/Users/Details/{id}` | Authenticated | View user details |
| GET | `/Users/Create` | Admin | Show create user form |
| POST | `/Users/Create` | Admin | Create new user |
| GET | `/Users/Edit/{id}` | CanEditUser | Show edit user form |
| POST | `/Users/Edit/{id}` | CanEditUser | Update user |
| POST | `/Users/Delete/{id}` | Admin | Soft delete user |
| POST | `/Users/BulkDelete` | Admin | Bulk soft delete users |

### 3.2 Query Parameters

**GET `/Users` Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | int | 1 | Page number (1-based) |
| pageSize | int | 25 | Records per page (10, 25, 50, 100) |
| search | string | null | Search term (searches Email, Username, Role) |
| sortBy | string | "CreatedAt" | Column to sort by (Email, Username, Role, CreatedAt) |
| sortOrder | string | "desc" | Sort order ("asc" or "desc") |

**Example:**
```
/Users?page=2&pageSize=50&search=admin&sortBy=Username&sortOrder=asc
```

### 3.3 Request/Response Models

**Create User Request:**
```json
{
  "email": "user@example.com",
  "username": "newuser",
  "password": "SecureP@ssw0rd!",
  "confirmPassword": "SecureP@ssw0rd!",
  "role": "User",
  "profilePicture": "<IFormFile>"
}
```

**Bulk Delete Request:**
```json
{
  "userIds": [
    "user1@example.com",
    "user2@example.com",
    "user3@example.com"
  ]
}
```

**Bulk Delete Response:**
```json
{
  "success": true,
  "count": 3,
  "message": "3 users deleted successfully"
}
```

---

## 4. Database Specifications

### 4.1 Enhanced Users Table Schema

```sql
CREATE TABLE [dbo].[Users] (
    -- Primary Key (Existing)
    [UserId] NVARCHAR(128) NOT NULL PRIMARY KEY,  -- Email address

    -- Existing Columns
    [Username] NVARCHAR(100) NOT NULL,
    [Password] NVARCHAR(500) NOT NULL,            -- Increased for secure hashing
    [Role] NVARCHAR(50) NOT NULL,

    -- New Columns
    [IsActive] BIT NOT NULL DEFAULT 1,            -- User enabled/disabled
    [IsDeleted] BIT NOT NULL DEFAULT 0,           -- Soft delete flag
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [DeletedAt] DATETIME2 NULL,
    [ProfilePicture] NVARCHAR(500) NULL,          -- Relative path or URL
    [LastLoginAt] DATETIME2 NULL,                 -- Last successful login

    -- Constraints
    CONSTRAINT CK_Users_Role CHECK ([Role] IN ('Admin', 'User'))
);
```

### 4.2 Indexes

```sql
-- Performance Indexes
CREATE INDEX IX_Users_IsDeleted ON Users(IsDeleted);
CREATE INDEX IX_Users_CreatedAt ON Users(CreatedAt DESC);
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Role ON Users(Role);
CREATE INDEX IX_Users_UserId_Password ON Users(UserId, Password);  -- Login optimization

-- Full-Text Search Index (Optional - for advanced search)
CREATE FULLTEXT INDEX ON Users(Username, UserId)
    KEY INDEX PK_Users
    WITH CHANGE_TRACKING AUTO;
```

### 4.3 Data Integrity Rules

**Business Rules:**
1. `UserId` must be unique and valid email format
2. `Username` must be unique (case-insensitive)
3. `Role` must be either "Admin" or "User"
4. `IsDeleted` users should have `DeletedAt` timestamp
5. `UpdatedAt` should be set on every update
6. Default values: `IsActive = 1`, `IsDeleted = 0`, `CreatedAt = GETUTCDATE()`

### 4.4 Sample Data

```sql
-- Admin User (for testing)
INSERT INTO Users (UserId, Username, Password, Role, IsActive, IsDeleted, CreatedAt)
VALUES
(
    'admin@demo.com',
    'admin',
    '<SecurelyHashedPassword>',
    'Admin',
    1,
    0,
    GETUTCDATE()
);

-- Regular User (for testing)
INSERT INTO Users (UserId, Username, Password, Role, IsActive, IsDeleted, CreatedAt)
VALUES
(
    'user@demo.com',
    'demouser',
    '<SecurelyHashedPassword>',
    'User',
    1,
    0,
    GETUTCDATE()
);
```

---

## 5. Security Specifications

### 5.1 Password Requirements

**Complexity Rules:**
- Minimum length: 8 characters
- Maximum length: 100 characters
- Must contain:
  - At least one uppercase letter (A-Z)
  - At least one lowercase letter (a-z)
  - At least one digit (0-9)
  - At least one special character (@$!%*?&)

**Regular Expression:**
```regex
^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$
```

### 5.2 Password Hashing

**Algorithm:** PBKDF2 (via ASP.NET Core Identity PasswordHasher)
- **Key Derivation:** PBKDF2-HMAC-SHA256
- **Iterations:** 100,000+ (automatic)
- **Salt:** Unique per password (automatic)
- **Output Format:** Base64-encoded hash with embedded salt and algorithm version

**Implementation:**
```csharp
var hasher = new PasswordHasher<string>();
var hashedPassword = hasher.HashPassword(null, plainTextPassword);

// Verification
var result = hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
bool isValid = result == PasswordVerificationResult.Success;
```

### 5.3 Rate Limiting Configuration

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*/Users/Login",
        "Period": "1m",
        "Limit": 5
      },
      {
        "Endpoint": "*/Users/Register",
        "Period": "1h",
        "Limit": 3
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### 5.4 Authorization Policies

```csharp
// Policy Definitions
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanEditUser", policy =>
        policy.Requirements.Add(new CanEditUserRequirement()));

    options.AddPolicy("CanDeleteUser", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanViewUsers", policy =>
        policy.RequireAuthenticatedUser());
});
```

### 5.5 CSRF Protection

**Implementation:**
- All POST forms must include anti-forgery token
- `[ValidateAntiForgeryToken]` attribute on all POST actions
- Token embedded in forms via `@Html.AntiForgeryToken()`

**Token Verification:**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]  // Required on all POST actions
public async Task<IActionResult> Action(Model model)
{
    // Action implementation
}
```

---

## 6. Performance Requirements

### 6.1 Response Time Targets

| Operation | Target (P95) | Maximum |
|-----------|--------------|---------|
| User List (25 records) | 150ms | 300ms |
| User List (100 records) | 250ms | 500ms |
| Search Query | 200ms | 400ms |
| Create User | 300ms | 600ms |
| Edit User | 250ms | 500ms |
| Delete User | 200ms | 400ms |
| Bulk Delete (10 users) | 500ms | 1000ms |
| File Upload | 1000ms | 2000ms |

### 6.2 Scalability Requirements

**Concurrent Users:**
- Minimum: 100 concurrent users
- Target: 500 concurrent users
- Maximum: 1,000 concurrent users

**Data Volume:**
- Minimum: 100 users
- Target: 10,000 users
- Maximum: 100,000 users

**Query Optimization:**
- Use `AsNoTracking()` for all read-only queries
- Implement pagination (never load all records)
- Use composite indexes for frequently-joined columns
- Cache frequently-accessed data (e.g., role lists)

### 6.3 Database Query Performance

**Optimization Strategies:**

```csharp
// GOOD: AsNoTracking for read-only queries
var users = await _context.Users
    .AsNoTracking()
    .Where(u => !u.IsDeleted)
    .OrderByDescending(u => u.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// BAD: Loading all records into memory
var users = await _context.Users.ToListAsync();
var filtered = users.Where(u => !u.IsDeleted).ToList(); // Client-side filtering
```

**Composite Index Usage:**

```sql
-- Index on (IsDeleted, CreatedAt) for efficient filtering and sorting
CREATE INDEX IX_Users_IsDeleted_CreatedAt ON Users(IsDeleted, CreatedAt DESC);

-- Query that benefits from this index
SELECT * FROM Users
WHERE IsDeleted = 0
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY;
```

---

## 7. File Upload Specifications

### 7.1 Profile Picture Requirements

**Allowed Formats:**
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- WebP (.webp) - optional

**Size Limits:**
- Maximum file size: 5 MB (5,242,880 bytes)
- Recommended dimensions: 150x150px (square)
- Maximum dimensions: 2000x2000px

**Validation Rules:**
1. File extension validation
2. MIME type validation
3. Magic bytes validation (prevent file type spoofing)
4. File size validation
5. Image dimension validation (optional)

### 7.2 File Storage

**Development/Staging:**
- Location: `wwwroot/uploads/avatars/`
- Naming: `{userId}_{timestamp}.{extension}`
- Example: `admin@demo.com_20250113120000.jpg`

**Production:**
- Location: Azure Blob Storage / AWS S3 (cloud storage)
- URL: `https://cdn.example.com/avatars/{userId}_{timestamp}.{extension}`
- CDN: CloudFlare / Azure CDN for optimized delivery

### 7.3 File Upload Security

**Security Measures:**

```csharp
public class FileUploadValidator
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public static bool IsValidImageFile(IFormFile file)
    {
        // 1. Extension validation
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return false;

        // 2. MIME type validation
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        // 3. File size validation
        if (file.Length == 0 || file.Length > MaxFileSize)
            return false;

        // 4. Magic bytes validation
        return IsValidImageMagicBytes(file, extension);
    }

    private static bool IsValidImageMagicBytes(IFormFile file, string extension)
    {
        var signatures = new Dictionary<string, byte[][]>
        {
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } }
        };

        if (!signatures.ContainsKey(extension))
            return false;

        using var reader = new BinaryReader(file.OpenReadStream());
        var headerBytes = reader.ReadBytes(signatures[extension][0].Length);

        return signatures[extension].Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}
```

### 7.4 Image Processing (Optional Enhancement)

**Image Resizing:**
- Generate thumbnail: 150x150px (for table display)
- Generate medium: 400x400px (for profile page)
- Maintain aspect ratio

**Libraries:**
- `SixLabors.ImageSharp` (recommended)
- `System.Drawing.Common` (Windows only)

**Example Implementation:**

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public async Task<string> ResizeAndSaveImageAsync(IFormFile file, string savePath)
{
    using var image = await Image.LoadAsync(file.OpenReadStream());

    // Resize to 150x150 (thumbnail)
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(150, 150),
        Mode = ResizeMode.Crop
    }));

    await image.SaveAsJpegAsync(savePath);

    return savePath;
}
```

---

## 8. Error Handling

### 8.1 Error Response Format

**ModelState Errors (Validation):**
```json
{
  "errors": {
    "Email": ["Email is required", "Invalid email format"],
    "Password": ["Password must be at least 8 characters"]
  },
  "type": "ValidationError",
  "title": "One or more validation errors occurred"
}
```

**Service Layer Errors:**
```csharp
public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public string? ErrorCode { get; set; }  // e.g., "USER_NOT_FOUND", "DUPLICATE_EMAIL"
}
```

### 8.2 Exception Handling Strategy

**Global Exception Handler:**

```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = new
        {
            message = "An unexpected error occurred. Please try again later.",
            requestId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(error);
    }
});
```

**Controller-Level Exception Handling:**

```csharp
[HttpPost]
public async Task<IActionResult> Action(Model model)
{
    try
    {
        // Action logic
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error in {Action}", nameof(Action));
        ModelState.AddModelError("", "Database error. Please try again.");
        return View(model);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in {Action}", nameof(Action));
        ModelState.AddModelError("", "An unexpected error occurred.");
        return View(model);
    }
}
```

### 8.3 User-Friendly Error Messages

**Error Message Guidelines:**
- ✅ **DO:** "Unable to delete user. This user is currently active."
- ❌ **DON'T:** "Foreign key constraint violation on Users table."

- ✅ **DO:** "Email address is already registered. Please use a different email."
- ❌ **DON'T:** "Duplicate key error: IX_Users_UserId."

- ✅ **DO:** "Invalid file type. Please upload a JPG, PNG, or GIF image."
- ❌ **DON'T:** "MIME type validation failed: application/octet-stream."

### 8.4 Logging Standards

**Log Levels:**
- **Debug:** Development only, detailed execution flow
- **Information:** User actions (created user, updated profile)
- **Warning:** Validation failures, authorization denials
- **Error:** Exceptions, database errors
- **Critical:** Application crashes, data corruption

**Structured Logging Example:**

```csharp
_logger.LogInformation(
    "User {UserId} created by {AdminUserId} at {Timestamp}",
    newUser.UserId,
    currentUser.UserId,
    DateTime.UtcNow);

_logger.LogWarning(
    "Unauthorized edit attempt: User {UserId} tried to edit {TargetUserId}",
    currentUser.UserId,
    targetUserId);

_logger.LogError(
    ex,
    "Failed to delete user {UserId}. Error: {ErrorMessage}",
    userId,
    ex.Message);
```

---

## Appendix: Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<configured-via-user-secrets-or-environment>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "FileUpload": {
    "MaxFileSizeMB": 5,
    "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".gif" ],
    "UploadPath": "uploads/avatars"
  },
  "Pagination": {
    "DefaultPageSize": 25,
    "MaxPageSize": 100,
    "PageSizeOptions": [ 10, 25, 50, 100 ]
  },
  "AllowedHosts": "*"
}
```

---

**Document Status:** Approved - Ready for Implementation
**Next Review:** After Pre-Development Phase

**Related Documents:**
- Execution Plan: `Execution_Plan.md`
- Database Schema: `Database_Schema_Design.md`
- Testing Strategy: `Testing_Strategy.md`
