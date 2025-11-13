using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LoginandRegisterMVC.Controllers;

public class UsersController(
    UserContext context,
    ISecurePasswordHashService securePasswordHashService,
#pragma warning disable CS0618 // Type or member is obsolete
    IPasswordHashService legacyPasswordHashService,
#pragma warning restore CS0618
    ILogger<UsersController> logger) : Controller
{
    private readonly UserContext _context = context;
    private readonly ISecurePasswordHashService _securePasswordHashService = securePasswordHashService;
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IPasswordHashService _legacyPasswordHashService = legacyPasswordHashService;
#pragma warning restore CS0618
    private readonly ILogger<UsersController> _logger = logger;

    // GET: Users
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    public IActionResult Register()
    {
        return View(new User());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(User user)
    {
        var existingUser = await _context.Users
            .Where(u => u.UserId.Equals(user.UserId))
            .FirstOrDefaultAsync();

        if (existingUser == null)
        {
            if (ModelState.IsValid)
            {
                // SECURITY FIX: Enforce server-side role assignment to prevent privilege escalation
                // Always assign "User" role for new registrations, ignore any client-submitted role
                user.Role = "User";
                _logger.LogInformation("New user registration - role set to 'User' (server-side): {UserId}", user.UserId);

                // SECURITY FIX: Use secure PBKDF2 hashing for new registrations
                user.PasswordV2 = _securePasswordHashService.HashPassword(user.Password);
                user.PasswordMigrated = true;
                user.Password = _securePasswordHashService.HashPassword(user.Password); // Keep both during migration
                // DO NOT hash ConfirmPassword - it's just for validation

                _logger.LogInformation("New user registered with secure password hashing: {UserId}", user.UserId);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Error Occurred! Try again!!");
            }
        }
        else
        {
            ModelState.AddModelError("", "User exists, Please login with your password");
        }
        return View(user);
    }

    public async Task<IActionResult> Login()
    {
        var adminUser = await _context.Users
            .Where(u => u.UserId.Equals("admin@demo.com"))
            .FirstOrDefaultAsync();

        if (adminUser == null)
        {
            // SECURITY FIX: Create admin with secure password hashing
            var user = new User
            {
                UserId = "admin@demo.com",
                Username = "admin",
                PasswordV2 = _securePasswordHashService.HashPassword("Admin@123"),
                Password = _securePasswordHashService.HashPassword("Admin@123"),
                PasswordMigrated = true,
                Role = "Admin"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin user created with secure password hashing");
        }
        return View(new User());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(User user)
    {
        // SECURITY FIX: Fetch user first, then verify password with migration support
        var dbUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        if (dbUser == null)
        {
            ModelState.AddModelError("", "UserId or password wrong");
            return View(user);
        }

        bool isValid = false;

        // Check if user has migrated to secure password hashing
        if (dbUser.PasswordMigrated && !string.IsNullOrEmpty(dbUser.PasswordV2))
        {
            // Use new secure PBKDF2 verification
            isValid = _securePasswordHashService.VerifyPassword(dbUser.PasswordV2, user.Password);
            _logger.LogDebug("Login attempt using secure password hash for {UserId}", user.UserId);
        }
        else
        {
            // Fallback to legacy SHA1 verification
            var legacyHash = _legacyPasswordHashService.HashPassword(user.Password);
            isValid = dbUser.Password == legacyHash;

            if (isValid)
            {
                // AUTOMATIC MIGRATION: Upgrade password to secure hash on successful login
                dbUser.PasswordV2 = _securePasswordHashService.HashPassword(user.Password);
                dbUser.Password = dbUser.PasswordV2; // Keep both in sync
                dbUser.PasswordMigrated = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password automatically migrated to secure hash for {UserId}", dbUser.UserId);
            }
            else
            {
                _logger.LogWarning("Failed login attempt with legacy hash for {UserId}", user.UserId);
            }
        }

        if (!isValid)
        {
            ModelState.AddModelError("", "UserId or password wrong");
            return View(user);
        }

        // Authentication successful - create claims and sign in
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, dbUser.UserId),
            new(ClaimTypes.NameIdentifier, dbUser.UserId),
            new("Username", dbUser.Username),
            new(ClaimTypes.Role, dbUser.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);

        HttpContext.Session.SetString("UserId", dbUser.UserId);
        HttpContext.Session.SetString("Username", dbUser.Username);
        HttpContext.Session.SetString("Role", dbUser.Role);

        _logger.LogInformation("User {UserId} logged in successfully", dbUser.UserId);

        return RedirectToAction("Index");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
