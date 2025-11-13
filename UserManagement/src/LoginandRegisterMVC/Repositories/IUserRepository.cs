using LoginandRegisterMVC.Models;

namespace LoginandRegisterMVC.Repositories;

/// <summary>
/// Repository interface for User entity operations
/// Provides abstraction layer for data access
/// </summary>
public interface IUserRepository
{
    // Query Operations
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByIdIncludingDeletedAsync(string userId);
    Task<bool> UserExistsAsync(string userId);

    // Command Operations
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId); // Soft delete
    Task<bool> HardDeleteUserAsync(string userId); // Permanent delete
    Task<bool> RestoreUserAsync(string userId); // Restore soft-deleted user

    // Authentication Operations
    Task<User?> AuthenticateUserAsync(string userId, string hashedPassword);
    Task UpdateLastLoginAsync(string userId);

    // Utility Operations
    Task<int> GetUserCountAsync();
    Task<int> GetActiveUserCountAsync();
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
    Task<IEnumerable<User>> GetRecentUsersAsync(int count);
    Task<bool> SaveChangesAsync();
}
