using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginandRegisterMVC.Repositories;

/// <summary>
/// Repository implementation for User entity operations
/// Handles all data access logic for users
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(UserContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Query Operations

    /// <summary>
    /// Get all non-deleted users (uses global query filter)
    /// </summary>
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        _logger.LogDebug("Getting all users");
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get only active, non-deleted users
    /// </summary>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        _logger.LogDebug("Getting active users");
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get user by ID (excludes soft-deleted users)
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        _logger.LogDebug("Getting user by ID: {UserId}", userId);
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// Get user by ID including soft-deleted users (ignores global query filter)
    /// </summary>
    public async Task<User?> GetUserByIdIncludingDeletedAsync(string userId)
    {
        _logger.LogDebug("Getting user by ID including deleted: {UserId}", userId);
        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// Check if user exists (excludes soft-deleted users)
    /// </summary>
    public async Task<bool> UserExistsAsync(string userId)
    {
        return await _context.Users.AnyAsync(u => u.UserId == userId);
    }

    #endregion

    #region Command Operations

    /// <summary>
    /// Create a new user
    /// </summary>
    public async Task<User> CreateUserAsync(User user)
    {
        _logger.LogInformation("Creating new user: {UserId}", user.UserId);

        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.IsDeleted = false;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {UserId}", user.UserId);
        return user;
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    public async Task<User> UpdateUserAsync(User user)
    {
        _logger.LogInformation("Updating user: {UserId}", user.UserId);

        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", user.UserId);
        return user;
    }

    /// <summary>
    /// Soft delete a user (sets IsDeleted flag and DeletedAt timestamp)
    /// </summary>
    public async Task<bool> DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Soft deleting user: {UserId}", userId);

        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for deletion: {UserId}", userId);
            return false;
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User soft deleted successfully: {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Permanently delete a user from database
    /// </summary>
    public async Task<bool> HardDeleteUserAsync(string userId)
    {
        _logger.LogWarning("Hard deleting user (permanent): {UserId}", userId);

        var user = await GetUserByIdIncludingDeletedAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for hard deletion: {UserId}", userId);
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogWarning("User hard deleted permanently: {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Restore a soft-deleted user
    /// </summary>
    public async Task<bool> RestoreUserAsync(string userId)
    {
        _logger.LogInformation("Restoring soft-deleted user: {UserId}", userId);

        var user = await GetUserByIdIncludingDeletedAsync(userId);
        if (user == null || !user.IsDeleted)
        {
            _logger.LogWarning("User not found or not deleted: {UserId}", userId);
            return false;
        }

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User restored successfully: {UserId}", userId);
        return true;
    }

    #endregion

    #region Authentication Operations

    /// <summary>
    /// Authenticate user with hashed password
    /// </summary>
    public async Task<User?> AuthenticateUserAsync(string userId, string hashedPassword)
    {
        _logger.LogDebug("Authenticating user: {UserId}", userId);

        return await _context.Users
            .Where(u => u.UserId == userId && u.Password == hashedPassword && u.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    public async Task UpdateLastLoginAsync(string userId)
    {
        _logger.LogDebug("Updating last login for user: {UserId}", userId);

        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Utility Operations

    /// <summary>
    /// Get total count of non-deleted users
    /// </summary>
    public async Task<int> GetUserCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    /// <summary>
    /// Get count of active, non-deleted users
    /// </summary>
    public async Task<int> GetActiveUserCountAsync()
    {
        return await _context.Users.CountAsync(u => u.IsActive);
    }

    /// <summary>
    /// Search users by username or email
    /// </summary>
    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        _logger.LogDebug("Searching users with term: {SearchTerm}", searchTerm);

        return await _context.Users
            .Where(u => u.Username.Contains(searchTerm) || u.UserId.Contains(searchTerm))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get most recently created users
    /// </summary>
    public async Task<IEnumerable<User>> GetRecentUsersAsync(int count)
    {
        _logger.LogDebug("Getting {Count} recent users", count);

        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Save changes to database
    /// </summary>
    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            return false;
        }
    }

    #endregion
}
