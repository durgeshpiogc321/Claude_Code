using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Repositories;
using LoginandRegisterMVC.ViewModels;
using Microsoft.Extensions.Logging;

namespace LoginandRegisterMVC.Services;

/// <summary>
/// Service for user management business logic.
/// Provides an abstraction layer between controllers and repositories.
/// </summary>
public class UserService(
    IUserRepository userRepository,
    ISecurePasswordHashService securePasswordHashService,
#pragma warning disable CS0618
    IPasswordHashService legacyPasswordHashService,
#pragma warning restore CS0618
    IEmailValidationService emailValidationService,
    ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISecurePasswordHashService _securePasswordHashService = securePasswordHashService;
#pragma warning disable CS0618
    private readonly IPasswordHashService _legacyPasswordHashService = legacyPasswordHashService;
#pragma warning restore CS0618
    private readonly IEmailValidationService _emailValidationService = emailValidationService;
    private readonly ILogger<UserService> _logger = logger;

    #region User CRUD Operations

    public async Task<(bool Success, User? User, string? ErrorMessage)> CreateUserAsync(UserRegistrationViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Creating new user: {Email}", viewModel.Email);

            // Validate email format and domain
            var (isEmailValid, emailError) = _emailValidationService.ValidateEmail(viewModel.Email);
            if (!isEmailValid)
            {
                _logger.LogWarning("User creation failed: {Error}", emailError);
                return (false, null, emailError);
            }

            // Normalize email
            var normalizedEmail = _emailValidationService.NormalizeEmail(viewModel.Email);

            // Check if user already exists
            if (await _userRepository.UserExistsAsync(normalizedEmail))
            {
                _logger.LogWarning("User creation failed: Email {Email} already exists", normalizedEmail);
                return (false, null, "A user with this email address already exists.");
            }

            // Validate passwords match
            if (viewModel.Password != viewModel.ConfirmPassword)
            {
                _logger.LogWarning("User creation failed: Password mismatch for {Email}", normalizedEmail);
                return (false, null, "Passwords do not match.");
            }

            // Hash password using secure service
            var hashedPassword = _securePasswordHashService.HashPassword(viewModel.Password);

            // Create user entity
            var user = new User
            {
                UserId = normalizedEmail,
                Username = viewModel.Username,
                Password = hashedPassword,
                PasswordV2 = hashedPassword,
                PasswordMigrated = true,
                Role = "User", // Server-side role enforcement (security fix)
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateUserAsync(user);

            _logger.LogInformation("User created successfully: {Email}", normalizedEmail);
            return (true, user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", viewModel.Email);
            return (false, null, "An error occurred while creating the user. Please try again.");
        }
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(bool includeInactive = false, bool includeDeleted = false)
    {
        try
        {
            _logger.LogDebug("Getting all users (IncludeInactive: {IncludeInactive}, IncludeDeleted: {IncludeDeleted})",
                includeInactive, includeDeleted);

            if (includeDeleted)
            {
                // Get all users including deleted ones, then filter by active status if needed
                var allUsers = await _userRepository.GetAllUsersAsync();
                return includeInactive ? allUsers : allUsers.Where(u => u.IsActive);
            }

            return includeInactive
                ? await _userRepository.GetAllUsersAsync()
                : await _userRepository.GetActiveUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return Enumerable.Empty<User>();
        }
    }

    public async Task<User?> GetUserByIdAsync(string userId, bool includeDeleted = false)
    {
        try
        {
            _logger.LogDebug("Getting user by ID: {UserId} (IncludeDeleted: {IncludeDeleted})", userId, includeDeleted);

            return includeDeleted
                ? await _userRepository.GetUserByIdIncludingDeletedAsync(userId)
                : await _userRepository.GetUserByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting user details for: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            return new UserDetailsViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DeletedAt = user.DeletedAt,
                LastLoginAt = user.LastLoginAt,
                ProfilePicture = user.ProfilePicture,
                PasswordMigrated = user.PasswordMigrated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details: {UserId}", userId);
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(string userId, UserEditViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Updating user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Update failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            // Update username
            user.Username = viewModel.Username;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(viewModel.NewPassword))
            {
                if (viewModel.NewPassword != viewModel.ConfirmNewPassword)
                {
                    _logger.LogWarning("Update failed: Password mismatch for {UserId}", userId);
                    return (false, "Passwords do not match.");
                }

                var hashedPassword = _securePasswordHashService.HashPassword(viewModel.NewPassword);
                user.Password = hashedPassword;
                user.PasswordV2 = hashedPassword;
                user.PasswordMigrated = true;

                _logger.LogInformation("Password updated for user: {UserId}", userId);
            }

            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("User updated successfully: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", userId);
            return (false, "An error occurred while updating the user. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(string userId, bool hardDelete = false)
    {
        try
        {
            _logger.LogInformation("Deleting user: {UserId} (HardDelete: {HardDelete})", userId, hardDelete);

            // Prevent deletion of admin users
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Delete failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            if (user.Role == "Admin")
            {
                _logger.LogWarning("Delete failed: Cannot delete admin user: {UserId}", userId);
                return (false, "Cannot delete admin users.");
            }

            bool success;
            if (hardDelete)
            {
                success = await _userRepository.HardDeleteUserAsync(userId);
                _logger.LogInformation("User hard deleted: {UserId}", userId);
            }
            else
            {
                success = await _userRepository.DeleteUserAsync(userId);
                _logger.LogInformation("User soft deleted: {UserId}", userId);
            }

            return success
                ? (true, null)
                : (false, "Failed to delete user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            return (false, "An error occurred while deleting the user. Please try again.");
        }
    }

    #endregion

    #region User Activation/Deactivation

    public async Task<(bool Success, string? ErrorMessage)> ActivateUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Activating user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Activation failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            if (user.IsActive)
            {
                _logger.LogDebug("User already active: {UserId}", userId);
                return (true, null);
            }

            user.IsActive = true;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("User activated successfully: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user: {UserId}", userId);
            return (false, "An error occurred while activating the user. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deactivating user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Deactivation failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            // Prevent deactivation of admin users
            if (user.Role == "Admin")
            {
                _logger.LogWarning("Deactivation failed: Cannot deactivate admin user: {UserId}", userId);
                return (false, "Cannot deactivate admin users.");
            }

            if (!user.IsActive)
            {
                _logger.LogDebug("User already inactive: {UserId}", userId);
                return (true, null);
            }

            user.IsActive = false;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("User deactivated successfully: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
            return (false, "An error occurred while deactivating the user. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Restoring user: {UserId}", userId);

            var success = await _userRepository.RestoreUserAsync(userId);

            if (success)
            {
                _logger.LogInformation("User restored successfully: {UserId}", userId);
                return (true, null);
            }

            _logger.LogWarning("Restore failed: User not found or not deleted: {UserId}", userId);
            return (false, "User not found or not deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring user: {UserId}", userId);
            return (false, "An error occurred while restoring the user. Please try again.");
        }
    }

    #endregion

    #region Authentication & Password Management

    public async Task<(bool Success, User? User, string? ErrorMessage)> AuthenticateUserAsync(UserLoginViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Authenticating user: {Email}", viewModel.Email);

            // Normalize email
            var normalizedEmail = _emailValidationService.NormalizeEmail(viewModel.Email);

            // Get user
            var user = await _userRepository.GetUserByIdAsync(normalizedEmail);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User not found: {Email}", normalizedEmail);
                return (false, null, "Invalid email or password.");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication failed: User inactive: {Email}", normalizedEmail);
                return (false, null, "Your account has been deactivated. Please contact support.");
            }

            // Verify password with migration support
            bool isPasswordValid = false;

            if (user.PasswordMigrated && !string.IsNullOrEmpty(user.PasswordV2))
            {
                // Use secure PBKDF2 verification
                isPasswordValid = _securePasswordHashService.VerifyPassword(user.PasswordV2, viewModel.Password);
                _logger.LogDebug("Password verified using secure hash for {Email}", normalizedEmail);
            }
            else
            {
                // Fallback to legacy SHA1 verification
                var legacyHash = _legacyPasswordHashService.HashPassword(viewModel.Password);
                isPasswordValid = user.Password == legacyHash;

                if (isPasswordValid)
                {
                    // Automatic password migration
                    user.PasswordV2 = _securePasswordHashService.HashPassword(viewModel.Password);
                    user.Password = user.PasswordV2;
                    user.PasswordMigrated = true;
                    await _userRepository.UpdateUserAsync(user);

                    _logger.LogInformation("Password automatically migrated to secure hash for {Email}", normalizedEmail);
                }
            }

            if (!isPasswordValid)
            {
                _logger.LogWarning("Authentication failed: Invalid password for {Email}", normalizedEmail);
                return (false, null, "Invalid email or password.");
            }

            // Update last login timestamp
            await _userRepository.UpdateLastLoginAsync(user.UserId);

            _logger.LogInformation("User authenticated successfully: {Email}", normalizedEmail);
            return (true, user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user: {Email}", viewModel.Email);
            return (false, null, "An error occurred during authentication. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            _logger.LogInformation("Changing password for user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            // Verify current password
            bool isCurrentPasswordValid;
            if (user.PasswordMigrated && !string.IsNullOrEmpty(user.PasswordV2))
            {
                isCurrentPasswordValid = _securePasswordHashService.VerifyPassword(user.PasswordV2, currentPassword);
            }
            else
            {
                var legacyHash = _legacyPasswordHashService.HashPassword(currentPassword);
                isCurrentPasswordValid = user.Password == legacyHash;
            }

            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Password change failed: Current password incorrect for {UserId}", userId);
                return (false, "Current password is incorrect.");
            }

            // Hash new password
            var hashedPassword = _securePasswordHashService.HashPassword(newPassword);
            user.Password = hashedPassword;
            user.PasswordV2 = hashedPassword;
            user.PasswordMigrated = true;

            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return (false, "An error occurred while changing the password. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(string userId, string newPassword)
    {
        try
        {
            _logger.LogInformation("Resetting password for user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            // Hash new password
            var hashedPassword = _securePasswordHashService.HashPassword(newPassword);
            user.Password = hashedPassword;
            user.PasswordV2 = hashedPassword;
            user.PasswordMigrated = true;

            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Password reset successfully for user: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
            return (false, "An error occurred while resetting the password. Please try again.");
        }
    }

    #endregion

    #region Search & Filtering

    public async Task<UserListPageViewModel> SearchUsersAsync(string? searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogDebug("Searching users: {SearchTerm} (Page: {Page}, Size: {Size})", searchTerm, pageNumber, pageSize);

            IEnumerable<User> users;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                users = await _userRepository.GetAllUsersAsync();
            }
            else
            {
                users = await _userRepository.SearchUsersAsync(searchTerm);
            }

            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new UserListPageViewModel
            {
                Users = pagedUsers,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalUsers = totalCount,
                SearchTerm = searchTerm
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
            return new UserListPageViewModel { Users = new List<User>() };
        }
    }

    public async Task<UserListPageViewModel> GetFilteredUsersAsync(
        string? role = null,
        bool? isActive = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            _logger.LogDebug("Getting filtered users (Role: {Role}, Active: {Active}, Sort: {Sort}, Page: {Page})",
                role, isActive, sortBy, pageNumber);

            var users = await _userRepository.GetAllUsersAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue)
            {
                users = users.Where(u => u.IsActive == isActive.Value);
            }

            // Apply sorting
            users = sortBy.ToLowerInvariant() switch
            {
                "username" => sortDescending ? users.OrderByDescending(u => u.Username) : users.OrderBy(u => u.Username),
                "role" => sortDescending ? users.OrderByDescending(u => u.Role) : users.OrderBy(u => u.Role),
                "lastloginat" => sortDescending ? users.OrderByDescending(u => u.LastLoginAt) : users.OrderBy(u => u.LastLoginAt),
                _ => sortDescending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt)
            };

            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new UserListPageViewModel
            {
                Users = pagedUsers,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalUsers = totalCount,
                RoleFilter = role,
                ActiveFilter = isActive,
                SortBy = sortBy,
                SortOrder = sortDescending ? "desc" : "asc"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered users");
            return new UserListPageViewModel { Users = new List<User>() };
        }
    }

    public async Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10)
    {
        try
        {
            _logger.LogDebug("Getting {Count} recent users", count);
            return await _userRepository.GetRecentUsersAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent users");
            return Enumerable.Empty<User>();
        }
    }

    #endregion

    #region Statistics & Reporting

    public async Task<int> GetUserCountAsync(bool activeOnly = false)
    {
        try
        {
            _logger.LogDebug("Getting user count (ActiveOnly: {ActiveOnly})", activeOnly);

            return activeOnly
                ? await _userRepository.GetActiveUserCountAsync()
                : await _userRepository.GetUserCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user count");
            return 0;
        }
    }

    public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
    {
        try
        {
            _logger.LogDebug("Getting user statistics");

            var allUsers = await _userRepository.GetAllUsersAsync();
            var statistics = new Dictionary<string, int>
            {
                ["TotalUsers"] = allUsers.Count(),
                ["ActiveUsers"] = allUsers.Count(u => u.IsActive),
                ["InactiveUsers"] = allUsers.Count(u => !u.IsActive),
                ["AdminUsers"] = allUsers.Count(u => u.Role == "Admin"),
                ["RegularUsers"] = allUsers.Count(u => u.Role == "User"),
                ["MigratedPasswords"] = allUsers.Count(u => u.PasswordMigrated),
                ["LegacyPasswords"] = allUsers.Count(u => !u.PasswordMigrated),
                ["UsersWithProfilePictures"] = allUsers.Count(u => !string.IsNullOrEmpty(u.ProfilePicture))
            };

            _logger.LogInformation("User statistics: {Statistics}", statistics);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetUserRegistrationTrendAsync(int days = 30)
    {
        try
        {
            _logger.LogDebug("Getting user registration trend for last {Days} days", days);

            var allUsers = await _userRepository.GetAllUsersAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var trend = new Dictionary<string, int>();

            // Initialize all days with 0
            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd");
                trend[date] = 0;
            }

            // Count registrations per day
            var registrations = allUsers
                .Where(u => u.CreatedAt >= cutoffDate)
                .GroupBy(u => u.CreatedAt.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            // Merge with initialized days
            foreach (var kvp in registrations)
            {
                if (trend.ContainsKey(kvp.Key))
                {
                    trend[kvp.Key] = kvp.Value;
                }
            }

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user registration trend");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetLoginActivityTrendAsync(int days = 30)
    {
        try
        {
            _logger.LogDebug("Getting login activity trend for last {Days} days", days);

            var allUsers = await _userRepository.GetAllUsersAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var trend = new Dictionary<string, int>();

            // Initialize all days with 0
            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd");
                trend[date] = 0;
            }

            // Count logins per day
            var logins = allUsers
                .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= cutoffDate)
                .GroupBy(u => u.LastLoginAt!.Value.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            // Merge with initialized days
            foreach (var kvp in logins)
            {
                if (trend.ContainsKey(kvp.Key))
                {
                    trend[kvp.Key] = kvp.Value;
                }
            }

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting login activity trend");
            return new Dictionary<string, int>();
        }
    }

    #endregion

    #region Validation

    public async Task<bool> UserExistsAsync(string userId)
    {
        try
        {
            return await _userRepository.UserExistsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        try
        {
            var normalizedEmail = _emailValidationService.NormalizeEmail(email);
            return !await _userRepository.UserExistsAsync(normalizedEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability: {Email}", email);
            return false;
        }
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null)
    {
        try
        {
            var allUsers = await _userRepository.GetAllUsersAsync();
            return !allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                                      && u.UserId != excludeUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability: {Username}", username);
            return false;
        }
    }

    #endregion

    #region Profile Management

    public async Task<(bool Success, string? ErrorMessage)> UpdateProfilePictureAsync(string userId, string profilePicturePath)
    {
        try
        {
            _logger.LogInformation("Updating profile picture for user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile picture update failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            user.ProfilePicture = profilePicturePath;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Profile picture updated for user: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile picture for user: {UserId}", userId);
            return (false, "An error occurred while updating the profile picture. Please try again.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RemoveProfilePictureAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Removing profile picture for user: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile picture removal failed: User not found: {UserId}", userId);
                return (false, "User not found.");
            }

            user.ProfilePicture = null;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("Profile picture removed for user: {UserId}", userId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile picture for user: {UserId}", userId);
            return (false, "An error occurred while removing the profile picture. Please try again.");
        }
    }

    #endregion

    #region Export Operations

    public async Task<byte[]> ExportUsersToCsvAsync(string? role = null, bool? isActive = null, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Exporting users to CSV (Role: {Role}, Active: {IsActive}, Search: {SearchTerm})",
                role, isActive, searchTerm);

            // Get filtered users
            var users = await GetFilteredUsersForExportAsync(role, isActive, searchTerm);

            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            // Define export data with selected columns
            var exportData = users.Select(u => new
            {
                Email = u.UserId,
                Username = u.Username,
                Role = u.Role,
                IsActive = u.IsActive ? "Active" : "Inactive",
                CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                LastLoginAt = u.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                HasProfilePicture = !string.IsNullOrEmpty(u.ProfilePicture) ? "Yes" : "No"
            }).ToList();

            csvWriter.WriteRecords(exportData);
            await streamWriter.FlushAsync();

            _logger.LogInformation("Successfully exported {Count} users to CSV", exportData.Count);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportUsersToExcelAsync(string? role = null, bool? isActive = null, string? searchTerm = null)
    {
        try
        {
            _logger.LogInformation("Exporting users to Excel (Role: {Role}, Active: {IsActive}, Search: {SearchTerm})",
                role, isActive, searchTerm);

            // Get filtered users
            var users = await GetFilteredUsersForExportAsync(role, isActive, searchTerm);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");

            // Add headers
            worksheet.Cell(1, 1).Value = "Email";
            worksheet.Cell(1, 2).Value = "Username";
            worksheet.Cell(1, 3).Value = "Role";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Created At";
            worksheet.Cell(1, 6).Value = "Last Login";
            worksheet.Cell(1, 7).Value = "Profile Picture";

            // Style headers
            var headerRow = worksheet.Range(1, 1, 1, 7);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add data
            int row = 2;
            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value = user.UserId;
                worksheet.Cell(row, 2).Value = user.Username;
                worksheet.Cell(row, 3).Value = user.Role;
                worksheet.Cell(row, 4).Value = user.IsActive ? "Active" : "Inactive";
                worksheet.Cell(row, 5).Value = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 6).Value = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";
                worksheet.Cell(row, 7).Value = !string.IsNullOrEmpty(user.ProfilePicture) ? "Yes" : "No";

                // Color code status
                if (user.IsActive)
                {
                    worksheet.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                }
                else
                {
                    worksheet.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add filter to headers
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
            {
                usedRange.SetAutoFilter();
            }

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);

            _logger.LogInformation("Successfully exported {Count} users to Excel", users.Count());
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            throw;
        }
    }

    /// <summary>
    /// Helper method to get filtered users for export operations
    /// </summary>
    private async Task<IEnumerable<User>> GetFilteredUsersForExportAsync(string? role, bool? isActive, string? searchTerm)
    {
        var allUsers = await _userRepository.GetAllUsersAsync();

        // Apply filters
        var filteredUsers = allUsers.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(role))
        {
            filteredUsers = filteredUsers.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredUsers = filteredUsers.Where(u =>
                u.UserId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return filteredUsers.OrderByDescending(u => u.CreatedAt).ToList();
    }

    #endregion
}
