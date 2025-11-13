using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.ViewModels;

namespace LoginandRegisterMVC.Services;

/// <summary>
/// Service interface for user management business logic.
/// Provides an abstraction layer between controllers and repositories.
/// </summary>
public interface IUserService
{
    #region User CRUD Operations

    /// <summary>
    /// Creates a new user with validation and business rules.
    /// </summary>
    /// <param name="viewModel">User registration data</param>
    /// <returns>Tuple containing success status, created user (if successful), and error message (if failed)</returns>
    Task<(bool Success, User? User, string? ErrorMessage)> CreateUserAsync(UserRegistrationViewModel viewModel);

    /// <summary>
    /// Gets all users with optional filtering.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users</param>
    /// <param name="includeDeleted">Whether to include soft-deleted users</param>
    /// <returns>List of users</returns>
    Task<IEnumerable<User>> GetAllUsersAsync(bool includeInactive = false, bool includeDeleted = false);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID (email)</param>
    /// <param name="includeDeleted">Whether to include soft-deleted users</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(string userId, bool includeDeleted = false);

    /// <summary>
    /// Gets user details formatted for display.
    /// </summary>
    /// <param name="userId">User ID (email)</param>
    /// <returns>UserDetailsViewModel if found, null otherwise</returns>
    Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId);

    /// <summary>
    /// Updates user information with validation.
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="viewModel">Updated user data</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(string userId, UserEditViewModel viewModel);

    /// <summary>
    /// Deletes a user (soft delete by default).
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="hardDelete">Whether to permanently delete (true) or soft delete (false)</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(string userId, bool hardDelete = false);

    #endregion

    #region User Activation/Deactivation

    /// <summary>
    /// Activates a user account.
    /// </summary>
    /// <param name="userId">User ID to activate</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> ActivateUserAsync(string userId);

    /// <summary>
    /// Deactivates a user account (prevents login without deleting data).
    /// </summary>
    /// <param name="userId">User ID to deactivate</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> DeactivateUserAsync(string userId);

    /// <summary>
    /// Restores a soft-deleted user.
    /// </summary>
    /// <param name="userId">User ID to restore</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> RestoreUserAsync(string userId);

    #endregion

    #region Authentication & Password Management

    /// <summary>
    /// Authenticates a user and updates last login timestamp.
    /// </summary>
    /// <param name="viewModel">Login credentials</param>
    /// <returns>Tuple containing success status, authenticated user (if successful), and error message (if failed)</returns>
    Task<(bool Success, User? User, string? ErrorMessage)> AuthenticateUserAsync(UserLoginViewModel viewModel);

    /// <summary>
    /// Changes a user's password with validation.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password for verification</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    /// <summary>
    /// Resets a user's password (admin function).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(string userId, string newPassword);

    #endregion

    #region Search & Filtering

    /// <summary>
    /// Searches users by username or email with pagination.
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of users</returns>
    Task<UserListPageViewModel> SearchUsersAsync(string? searchTerm, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Gets users with advanced filtering options.
    /// </summary>
    /// <param name="role">Filter by role (null for all)</param>
    /// <param name="isActive">Filter by active status (null for all)</param>
    /// <param name="sortBy">Sort column (e.g., "CreatedAt", "Username")</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated and filtered list of users</returns>
    Task<UserListPageViewModel> GetFilteredUsersAsync(
        string? role = null,
        bool? isActive = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets recently created users.
    /// </summary>
    /// <param name="count">Number of users to retrieve</param>
    /// <returns>List of recent users</returns>
    Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10);

    #endregion

    #region Statistics & Reporting

    /// <summary>
    /// Gets total user count.
    /// </summary>
    /// <param name="activeOnly">Count only active users</param>
    /// <returns>User count</returns>
    Task<int> GetUserCountAsync(bool activeOnly = false);

    /// <summary>
    /// Gets user statistics summary.
    /// </summary>
    /// <returns>Dictionary containing various user statistics</returns>
    Task<Dictionary<string, int>> GetUserStatisticsAsync();

    /// <summary>
    /// Gets user registration trend data for the last N days.
    /// </summary>
    /// <param name="days">Number of days to analyze</param>
    /// <returns>Dictionary with date as key and count as value</returns>
    Task<Dictionary<string, int>> GetUserRegistrationTrendAsync(int days = 30);

    /// <summary>
    /// Gets user login activity for the last N days.
    /// </summary>
    /// <param name="days">Number of days to analyze</param>
    /// <returns>Dictionary with date as key and count as value</returns>
    Task<Dictionary<string, int>> GetLoginActivityTrendAsync(int days = 30);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a user exists.
    /// </summary>
    /// <param name="userId">User ID (email)</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> UserExistsAsync(string userId);

    /// <summary>
    /// Validates if an email is available for registration.
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <returns>True if available, false if already in use</returns>
    Task<bool> IsEmailAvailableAsync(string email);

    /// <summary>
    /// Validates if a username is available.
    /// </summary>
    /// <param name="username">Username to validate</param>
    /// <param name="excludeUserId">User ID to exclude from check (for updates)</param>
    /// <returns>True if available, false if already in use</returns>
    Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null);

    #endregion

    #region Profile Management

    /// <summary>
    /// Updates user's profile picture.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="profilePicturePath">Path or URL to profile picture</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateProfilePictureAsync(string userId, string profilePicturePath);

    /// <summary>
    /// Removes user's profile picture.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Tuple containing success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage)> RemoveProfilePictureAsync(string userId);

    #endregion

    #region Export Operations

    /// <summary>
    /// Exports filtered user data to CSV format.
    /// </summary>
    /// <param name="role">Filter by role (null for all)</param>
    /// <param name="isActive">Filter by active status (null for all)</param>
    /// <param name="searchTerm">Search term to filter users</param>
    /// <returns>CSV file content as byte array</returns>
    Task<byte[]> ExportUsersToCsvAsync(string? role = null, bool? isActive = null, string? searchTerm = null);

    /// <summary>
    /// Exports filtered user data to Excel format.
    /// </summary>
    /// <param name="role">Filter by role (null for all)</param>
    /// <param name="isActive">Filter by active status (null for all)</param>
    /// <param name="searchTerm">Search term to filter users</param>
    /// <returns>Excel file content as byte array</returns>
    Task<byte[]> ExportUsersToExcelAsync(string? role = null, bool? isActive = null, string? searchTerm = null);

    #endregion
}
