using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Repositories;
using LoginandRegisterMVC.Services;
using LoginandRegisterMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LoginandRegisterMVC.Controllers;

/// <summary>
/// Controller for user management operations.
/// Uses service layer for business logic and ViewModels for data transfer.
/// </summary>
public class UsersController(
    IUserService userService,
    IUserRepository userRepository,
    ISecurePasswordHashService securePasswordHashService,
    UserContext context,
    IWebHostEnvironment hostEnvironment,
    ILogger<UsersController> logger) : Controller
{
    private readonly IUserService _userService = userService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISecurePasswordHashService _securePasswordHashService = securePasswordHashService;
    private readonly UserContext _context = context;
    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ILogger<UsersController> _logger = logger;

    #region Dashboard

    /// <summary>
    /// GET: /Users/Dashboard - Displays analytics dashboard with charts and statistics
    /// </summary>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            _logger.LogInformation("Loading analytics dashboard");

            // Get statistics
            var statistics = await _userService.GetUserStatisticsAsync();

            // Get trends
            var registrationTrend = await _userService.GetUserRegistrationTrendAsync(30);
            var loginActivityTrend = await _userService.GetLoginActivityTrendAsync(30);

            // Get recent users
            var recentUsers = await _userService.GetRecentUsersAsync(5);

            // Pass data to view via ViewData
            ViewData["Statistics"] = statistics;
            ViewData["RegistrationTrend"] = registrationTrend;
            ViewData["LoginActivityTrend"] = loginActivityTrend;
            ViewData["RecentUsers"] = recentUsers;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics dashboard");
            TempData["Error"] = "An error occurred while loading the dashboard.";
            return RedirectToAction("Index");
        }
    }

    #endregion

    #region User List

    /// <summary>
    /// GET: /Users/Index - Displays list of all users with search, filter, sort, and pagination (requires authorization)
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? roleFilter = null,
        bool? activeFilter = null,
        string sortBy = "CreatedAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Loading user list with filters - Search: {Search}, Role: {Role}, Active: {Active}, Sort: {Sort} {Order}, Page: {Page}",
                searchTerm, roleFilter, activeFilter, sortBy, sortOrder, pageNumber);

            // If search term is provided, use search functionality
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchResult = await _userService.SearchUsersAsync(searchTerm, pageNumber, pageSize);

                // Store search parameters in ViewData for form retention
                ViewData["SearchTerm"] = searchTerm;
                ViewData["CurrentPage"] = pageNumber;
                ViewData["TotalPages"] = searchResult.TotalPages;
                ViewData["TotalUsers"] = searchResult.TotalUsers;

                return View(searchResult.Users);
            }

            // Otherwise use filter functionality
            bool sortDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
            var filterResult = await _userService.GetFilteredUsersAsync(
                role: roleFilter,
                isActive: activeFilter,
                sortBy: sortBy,
                sortDescending: sortDescending,
                pageNumber: pageNumber,
                pageSize: pageSize);

            // Store filter parameters in ViewData for form retention
            ViewData["RoleFilter"] = roleFilter;
            ViewData["ActiveFilter"] = activeFilter?.ToString().ToLower();
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = filterResult.TotalPages;
            ViewData["TotalUsers"] = filterResult.TotalUsers;
            ViewData["PageSize"] = pageSize;

            return View(filterResult.Users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user list");
            TempData["Error"] = "An error occurred while loading the user list.";

            // Return empty list with default ViewData
            ViewData["CurrentPage"] = 1;
            ViewData["TotalPages"] = 0;
            ViewData["TotalUsers"] = 0;

            return View(new List<User>());
        }
    }

    #endregion

    #region Registration

    /// <summary>
    /// GET: /Users/Register - Displays registration form
    /// </summary>
    public IActionResult Register()
    {
        _logger.LogDebug("Registration form requested");
        return View(new UserRegistrationViewModel());
    }

    /// <summary>
    /// POST: /Users/Register - Processes user registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(UserRegistrationViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration failed: Invalid model state for {Email}", viewModel.Email);
                return View(viewModel);
            }

            // Use service layer for user creation
            var (success, user, errorMessage) = await _userService.CreateUserAsync(viewModel);

            if (!success)
            {
                _logger.LogWarning("Registration failed for {Email}: {Error}", viewModel.Email, errorMessage);
                ModelState.AddModelError("", errorMessage ?? "Registration failed. Please try again.");
                return View(viewModel);
            }

            _logger.LogInformation("User registered successfully: {Email}", viewModel.Email);
            TempData["Success"] = "Registration successful! Please login with your credentials.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", viewModel.Email);
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            return View(viewModel);
        }
    }

    #endregion

    #region Login

    /// <summary>
    /// GET: /Users/Login - Displays login form and ensures admin user exists
    /// </summary>
    public async Task<IActionResult> Login()
    {
        try
        {
            // Ensure admin user exists for demo purposes
            var adminExists = await _userService.UserExistsAsync("admin@demo.com");
            if (!adminExists)
            {
                _logger.LogInformation("Creating demo admin user");

                // Create admin user directly using repository (special case)
                var adminUser = new User
                {
                    UserId = "admin@demo.com",
                    Username = "admin",
                    Password = _securePasswordHashService.HashPassword("Admin@123"),
                    PasswordV2 = _securePasswordHashService.HashPassword("Admin@123"),
                    PasswordMigrated = true,
                    Role = "Admin",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.CreateUserAsync(adminUser);
                _logger.LogInformation("Demo admin user created successfully");
            }

            return View(new UserLoginViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Login GET action");
            return View(new UserLoginViewModel());
        }
    }

    /// <summary>
    /// POST: /Users/Login - Processes user login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UserLoginViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Invalid model state for {Email}", viewModel.Email);
                return View(viewModel);
            }

            // Use service layer for authentication
            var (success, user, errorMessage) = await _userService.AuthenticateUserAsync(viewModel);

            if (!success || user == null)
            {
                _logger.LogWarning("Authentication failed for {Email}: {Error}", viewModel.Email, errorMessage);
                ModelState.AddModelError("", errorMessage ?? "Invalid email or password.");
                return View(viewModel);
            }

            // Authentication successful - create claims and sign in
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserId),
                new(ClaimTypes.NameIdentifier, user.UserId),
                new("Username", user.Username),
                new(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = viewModel.RememberMe,
                ExpiresUtc = viewModel.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Set session values for backward compatibility
            HttpContext.Session.SetString("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            _logger.LogInformation("User {UserId} logged in successfully", user.UserId);
            TempData["Success"] = $"Welcome back, {user.Username}!";

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", viewModel.Email);
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            return View(viewModel);
        }
    }

    #endregion

    #region Logout

    /// <summary>
    /// GET: /Users/Logout - Signs out the current user
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {UserId} logging out", userId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return RedirectToAction("Login");
        }
    }

    #endregion

    #region User Details

    /// <summary>
    /// GET: /Users/Details/id - Displays detailed information about a user
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Details(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var userDetails = await _userService.GetUserDetailsAsync(id);

            if (userDetails == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            return View(userDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for {UserId}", id);
            TempData["Error"] = "An error occurred while loading user details.";
            return RedirectToAction("Index");
        }
    }

    #endregion

    #region User Edit

    /// <summary>
    /// GET: /Users/Edit/id - Displays edit form for a user
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var viewModel = new UserEditViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                CurrentProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            // Load available roles for dropdown
            ViewBag.Roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit form for {UserId}", id);
            TempData["Error"] = "An error occurred while loading the edit form.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/Edit - Processes user edit
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(UserEditViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var (success, errorMessage) = await _userService.UpdateUserAsync(viewModel.UserId, viewModel);

            if (!success)
            {
                TempData["Error"] = errorMessage ?? "Failed to update user.";
                return View(viewModel);
            }

            TempData["Success"] = $"User {viewModel.Username} updated successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", viewModel.UserId);
            TempData["Error"] = "An unexpected error occurred while updating the user.";
            return View(viewModel);
        }
    }

    #endregion

    #region User Actions (Delete, Activate, Deactivate)

    /// <summary>
    /// POST: /Users/Delete - Soft deletes a user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var (success, errorMessage) = await _userService.DeleteUserAsync(userId, hardDelete: false);

            if (success)
            {
                TempData["Success"] = "User deleted successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Failed to delete user.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            TempData["Error"] = "An unexpected error occurred while deleting the user.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/Activate - Activates a user account
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var (success, errorMessage) = await _userService.ActivateUserAsync(userId);

            if (success)
            {
                TempData["Success"] = "User activated successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Failed to activate user.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            TempData["Error"] = "An unexpected error occurred while activating the user.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/Deactivate - Deactivates a user account
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            var (success, errorMessage) = await _userService.DeactivateUserAsync(userId);

            if (success)
            {
                TempData["Success"] = "User deactivated successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Failed to deactivate user.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            TempData["Error"] = "An unexpected error occurred while deactivating the user.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/BulkActivate - Activates multiple users
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkActivate(List<string> userIds)
    {
        try
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Bulk activating {Count} users", userIds.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var userId in userIds)
            {
                var (success, errorMessage) = await _userService.ActivateUserAsync(userId);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("Failed to activate user {UserId}: {Error}", userId, errorMessage);
                }
            }

            if (successCount > 0)
            {
                TempData["Success"] = $"Successfully activated {successCount} user(s)!";
            }

            if (failCount > 0)
            {
                TempData["Error"] = $"Failed to activate {failCount} user(s).";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk activate");
            TempData["Error"] = "An unexpected error occurred during bulk activation.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/BulkDeactivate - Deactivates multiple users
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDeactivate(List<string> userIds)
    {
        try
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Bulk deactivating {Count} users", userIds.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var userId in userIds)
            {
                var (success, errorMessage) = await _userService.DeactivateUserAsync(userId);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("Failed to deactivate user {UserId}: {Error}", userId, errorMessage);
                }
            }

            if (successCount > 0)
            {
                TempData["Success"] = $"Successfully deactivated {successCount} user(s)!";
            }

            if (failCount > 0)
            {
                TempData["Error"] = $"Failed to deactivate {failCount} user(s).";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk deactivate");
            TempData["Error"] = "An unexpected error occurred during bulk deactivation.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Users/BulkDelete - Soft deletes multiple users
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(List<string> userIds)
    {
        try
        {
            if (userIds == null || !userIds.Any())
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Bulk deleting {Count} users", userIds.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var userId in userIds)
            {
                var (success, errorMessage) = await _userService.DeleteUserAsync(userId, hardDelete: false);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("Failed to delete user {UserId}: {Error}", userId, errorMessage);
                }
            }

            if (successCount > 0)
            {
                TempData["Success"] = $"Successfully deleted {successCount} user(s)!";
            }

            if (failCount > 0)
            {
                TempData["Error"] = $"Failed to delete {failCount} user(s).";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete");
            TempData["Error"] = "An unexpected error occurred during bulk deletion.";
            return RedirectToAction("Index");
        }
    }

    #endregion

    #region Export Operations

    /// <summary>
    /// GET: /Users/ExportToCsv - Exports filtered users to CSV format
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportToCsv(string? role = null, bool? isActive = null, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Admin exporting users to CSV (Role: {Role}, Active: {IsActive}, Search: {SearchTerm})",
                role, isActive, searchTerm);

            var csvData = await _userService.ExportUsersToCsvAsync(role, isActive, searchTerm);
            var fileName = $"Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation("CSV export successful, file size: {Size} bytes", csvData.Length);

            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to CSV");
            TempData["Error"] = "An error occurred while exporting users to CSV.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// GET: /Users/ExportToExcel - Exports filtered users to Excel format
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportToExcel(string? role = null, bool? isActive = null, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Admin exporting users to Excel (Role: {Role}, Active: {IsActive}, Search: {SearchTerm})",
                role, isActive, searchTerm);

            var excelData = await _userService.ExportUsersToExcelAsync(role, isActive, searchTerm);
            var fileName = $"Users_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Excel export successful, file size: {Size} bytes", excelData.Length);

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            TempData["Error"] = "An error occurred while exporting users to Excel.";
            return RedirectToAction("Index");
        }
    }

    #endregion

    #region Profile Picture Operations

    /// <summary>
    /// POST: /Users/UploadProfilePicture - Uploads a profile picture for a user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadProfilePicture(string userId, IFormFile profilePicture)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image file to upload.";
                return RedirectToAction("Edit", new { id = userId });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Invalid file type. Only JPG, JPEG, PNG, and GIF files are allowed.";
                return RedirectToAction("Edit", new { id = userId });
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (profilePicture.Length > maxFileSize)
            {
                TempData["Error"] = "File size exceeds 5MB limit.";
                return RedirectToAction("Edit", new { id = userId });
            }

            // Validate image content
            try
            {
                using var image = System.Drawing.Image.FromStream(profilePicture.OpenReadStream());
                // If we get here, it's a valid image
            }
            catch
            {
                TempData["Error"] = "The uploaded file is not a valid image.";
                return RedirectToAction("Edit", new { id = userId });
            }

            // Generate unique filename
            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "profile-pictures");
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Ensure directory exists
            Directory.CreateDirectory(uploadsFolder);

            // Get existing user to check for old profile picture
            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.ProfilePicture))
            {
                // Delete old profile picture
                var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                    _logger.LogInformation("Deleted old profile picture: {OldFile}", oldFilePath);
                }
            }

            // Save new file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Profile picture uploaded: {FileName} for user {UserId}", uniqueFileName, userId);

            // Update user profile picture path in database
            var relativePath = $"/uploads/profile-pictures/{uniqueFileName}";
            var (success, errorMessage) = await _userService.UpdateProfilePictureAsync(userId, relativePath);

            if (success)
            {
                TempData["Success"] = "Profile picture uploaded successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Failed to update profile picture.";
            }

            return RedirectToAction("Edit", new { id = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user: {UserId}", userId);
            TempData["Error"] = "An error occurred while uploading the profile picture.";
            return RedirectToAction("Edit", new { id = userId });
        }
    }

    /// <summary>
    /// POST: /Users/RemoveProfilePicture - Removes a user's profile picture
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveProfilePicture(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Removing profile picture for user: {UserId}", userId);

            // Get user to find profile picture path
            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.ProfilePicture))
            {
                // Delete physical file
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Deleted profile picture file: {FilePath}", filePath);
                }
            }

            // Update database
            var (success, errorMessage) = await _userService.RemoveProfilePictureAsync(userId);

            if (success)
            {
                TempData["Success"] = "Profile picture removed successfully!";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Failed to remove profile picture.";
            }

            return RedirectToAction("Edit", new { id = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile picture for user: {UserId}", userId);
            TempData["Error"] = "An error occurred while removing the profile picture.";
            return RedirectToAction("Edit", new { id = userId });
        }
    }

    #endregion
}
