using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LoginandRegisterMVC.UnitTests.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    private UserContext _context = null!;
    private UserRepository _repository = null!;
    private Mock<ILogger<UserRepository>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        // Create in-memory database with unique name for each test
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UserContext(options);
        _loggerMock = new Mock<ILogger<UserRepository>>();
        _repository = new UserRepository(_context, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateUserAsync Tests

    [Test]
    public async Task CreateUserAsync_Should_CreateUser_WithCorrectTimestamps()
    {
        // Arrange
        var user = new User
        {
            UserId = "test@example.com",
            Username = "testuser",
            Password = "hashedpassword",
            Role = "User"
        };

        // Act
        var result = await _repository.CreateUserAsync(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo("test@example.com"));
        Assert.That(result.IsActive, Is.True);
        Assert.That(result.IsDeleted, Is.False);
        Assert.That(result.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(result.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task CreateUserAsync_Should_SaveToDatabase()
    {
        // Arrange
        var user = new User
        {
            UserId = "test@example.com",
            Username = "testuser",
            Password = "hashedpassword",
            Role = "User"
        };

        // Act
        await _repository.CreateUserAsync(user);

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == "test@example.com");
        Assert.That(savedUser, Is.Not.Null);
        Assert.That(savedUser!.Username, Is.EqualTo("testuser"));
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Test]
    public async Task GetAllUsersAsync_Should_ReturnAllNonDeletedUsers()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var users = await _repository.GetAllUsersAsync();

        // Assert
        Assert.That(users.Count(), Is.EqualTo(2)); // Only non-deleted users
    }

    [Test]
    public async Task GetAllUsersAsync_Should_OrderByCreatedAtDescending()
    {
        // Arrange
        var user1 = new User
        {
            UserId = "user1@example.com",
            Username = "user1",
            Password = "hash1",
            Role = "User",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var user2 = new User
        {
            UserId = "user2@example.com",
            Username = "user2",
            Password = "hash2",
            Role = "User",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _repository.CreateUserAsync(user1);
        await _repository.CreateUserAsync(user2);

        // Act
        var users = (await _repository.GetAllUsersAsync()).ToList();

        // Assert
        Assert.That(users[0].UserId, Is.EqualTo("user2@example.com")); // Most recent first
        Assert.That(users[1].UserId, Is.EqualTo("user1@example.com"));
    }

    #endregion

    #region GetActiveUsersAsync Tests

    [Test]
    public async Task GetActiveUsersAsync_Should_ReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser = new User
        {
            UserId = "active@example.com",
            Username = "active",
            Password = "hash",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveUser = new User
        {
            UserId = "inactive@example.com",
            Username = "inactive",
            Password = "hash",
            Role = "User",
            IsActive = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add directly to context to bypass CreateUserAsync which sets IsActive = true
        await _context.Users.AddAsync(activeUser);
        await _context.Users.AddAsync(inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var users = await _repository.GetActiveUsersAsync();

        // Assert
        Assert.That(users.Count(), Is.EqualTo(1));
        Assert.That(users.First().UserId, Is.EqualTo("active@example.com"));
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Test]
    public async Task GetUserByIdAsync_Should_ReturnUser_WhenExists()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var user = await _repository.GetUserByIdAsync("user1@example.com");

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Username, Is.EqualTo("user1"));
    }

    [Test]
    public async Task GetUserByIdAsync_Should_ReturnNull_WhenNotExists()
    {
        // Act
        var user = await _repository.GetUserByIdAsync("nonexistent@example.com");

        // Assert
        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task GetUserByIdAsync_Should_ExcludeDeletedUsers()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var user = await _repository.GetUserByIdAsync("deleted@example.com");

        // Assert
        Assert.That(user, Is.Null); // Soft-deleted user should not be returned
    }

    #endregion

    #region GetUserByIdIncludingDeletedAsync Tests

    [Test]
    public async Task GetUserByIdIncludingDeletedAsync_Should_ReturnDeletedUser()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var user = await _repository.GetUserByIdIncludingDeletedAsync("deleted@example.com");

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.IsDeleted, Is.True);
    }

    #endregion

    #region UserExistsAsync Tests

    [Test]
    public async Task UserExistsAsync_Should_ReturnTrue_WhenUserExists()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var exists = await _repository.UserExistsAsync("user1@example.com");

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task UserExistsAsync_Should_ReturnFalse_WhenUserNotExists()
    {
        // Act
        var exists = await _repository.UserExistsAsync("nonexistent@example.com");

        // Assert
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task UserExistsAsync_Should_ReturnFalse_ForDeletedUser()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var exists = await _repository.UserExistsAsync("deleted@example.com");

        // Assert
        Assert.That(exists, Is.False); // Deleted users should not exist
    }

    #endregion

    #region UpdateUserAsync Tests

    [Test]
    public async Task UpdateUserAsync_Should_UpdateUser_AndSetUpdatedAt()
    {
        // Arrange
        var user = await CreateTestUser("update@example.com");
        var originalUpdatedAt = user.UpdatedAt;

        // Wait a bit to ensure timestamp changes
        await Task.Delay(10);

        user.Username = "updatedname";

        // Act
        var result = await _repository.UpdateUserAsync(user);

        // Assert
        Assert.That(result.Username, Is.EqualTo("updatedname"));
        Assert.That(result.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    #endregion

    #region DeleteUserAsync (Soft Delete) Tests

    [Test]
    public async Task DeleteUserAsync_Should_SoftDeleteUser()
    {
        // Arrange
        var user = await CreateTestUser("delete@example.com");

        // Act
        var result = await _repository.DeleteUserAsync("delete@example.com");

        // Assert
        Assert.That(result, Is.True);

        // Verify user is soft deleted
        var deletedUser = await _repository.GetUserByIdIncludingDeletedAsync("delete@example.com");
        Assert.That(deletedUser, Is.Not.Null);
        Assert.That(deletedUser!.IsDeleted, Is.True);
        Assert.That(deletedUser.DeletedAt, Is.Not.Null);
    }

    [Test]
    public async Task DeleteUserAsync_Should_ReturnFalse_WhenUserNotExists()
    {
        // Act
        var result = await _repository.DeleteUserAsync("nonexistent@example.com");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteUserAsync_Should_ExcludeFromNormalQueries()
    {
        // Arrange
        var user = await CreateTestUser("delete@example.com");

        // Act
        await _repository.DeleteUserAsync("delete@example.com");

        // Assert
        var users = await _repository.GetAllUsersAsync();
        Assert.That(users.Any(u => u.UserId == "delete@example.com"), Is.False);
    }

    #endregion

    #region RestoreUserAsync Tests

    [Test]
    public async Task RestoreUserAsync_Should_RestoreDeletedUser()
    {
        // Arrange
        var user = await CreateTestUser("restore@example.com");
        await _repository.DeleteUserAsync("restore@example.com");

        // Act
        var result = await _repository.RestoreUserAsync("restore@example.com");

        // Assert
        Assert.That(result, Is.True);

        var restoredUser = await _repository.GetUserByIdAsync("restore@example.com");
        Assert.That(restoredUser, Is.Not.Null);
        Assert.That(restoredUser!.IsDeleted, Is.False);
        Assert.That(restoredUser.DeletedAt, Is.Null);
    }

    [Test]
    public async Task RestoreUserAsync_Should_ReturnFalse_ForNonDeletedUser()
    {
        // Arrange
        await CreateTestUser("active@example.com");

        // Act
        var result = await _repository.RestoreUserAsync("active@example.com");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region HardDeleteUserAsync Tests

    [Test]
    public async Task HardDeleteUserAsync_Should_PermanentlyRemoveUser()
    {
        // Arrange
        var user = await CreateTestUser("harddelete@example.com");

        // Act
        var result = await _repository.HardDeleteUserAsync("harddelete@example.com");

        // Assert
        Assert.That(result, Is.True);

        var deletedUser = await _repository.GetUserByIdIncludingDeletedAsync("harddelete@example.com");
        Assert.That(deletedUser, Is.Null);
    }

    #endregion

    #region AuthenticateUserAsync Tests

    [Test]
    public async Task AuthenticateUserAsync_Should_ReturnUser_WithCorrectCredentials()
    {
        // Arrange
        var user = await CreateTestUser("auth@example.com", "correcthash");

        // Act
        var result = await _repository.AuthenticateUserAsync("auth@example.com", "correcthash");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo("auth@example.com"));
    }

    [Test]
    public async Task AuthenticateUserAsync_Should_ReturnNull_WithWrongPassword()
    {
        // Arrange
        await CreateTestUser("auth@example.com", "correcthash");

        // Act
        var result = await _repository.AuthenticateUserAsync("auth@example.com", "wronghash");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AuthenticateUserAsync_Should_ReturnNull_ForInactiveUser()
    {
        // Arrange
        var user = new User
        {
            UserId = "inactive@example.com",
            Username = "inactive",
            Password = "hash",
            Role = "User",
            IsActive = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        // Add directly to context to bypass CreateUserAsync which sets IsActive = true
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AuthenticateUserAsync("inactive@example.com", "hash");

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region UpdateLastLoginAsync Tests

    [Test]
    public async Task UpdateLastLoginAsync_Should_SetLastLoginTimestamp()
    {
        // Arrange
        var user = await CreateTestUser("login@example.com");
        Assert.That(user.LastLoginAt, Is.Null);

        // Act
        await _repository.UpdateLastLoginAsync("login@example.com");

        // Assert
        var updatedUser = await _repository.GetUserByIdAsync("login@example.com");
        Assert.That(updatedUser!.LastLoginAt, Is.Not.Null);
    }

    #endregion

    #region GetUserCountAsync Tests

    [Test]
    public async Task GetUserCountAsync_Should_ReturnCorrectCount()
    {
        // Arrange
        await SeedTestUsers();

        // Act
        var count = await _repository.GetUserCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(2)); // Only non-deleted users
    }

    #endregion

    #region GetActiveUserCountAsync Tests

    [Test]
    public async Task GetActiveUserCountAsync_Should_ReturnCorrectCount()
    {
        // Arrange
        var activeUser = new User
        {
            UserId = "active@example.com",
            Username = "active",
            Password = "hash",
            Role = "User",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveUser = new User
        {
            UserId = "inactive@example.com",
            Username = "inactive",
            Password = "hash",
            Role = "User",
            IsActive = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add directly to context to bypass CreateUserAsync which sets IsActive = true
        await _context.Users.AddAsync(activeUser);
        await _context.Users.AddAsync(inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetActiveUserCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(1));
    }

    #endregion

    #region SearchUsersAsync Tests

    [Test]
    public async Task SearchUsersAsync_Should_FindUsersByUsername()
    {
        // Arrange
        await CreateTestUser("john@example.com", "hash", "JohnDoe");
        await CreateTestUser("jane@example.com", "hash", "JaneSmith");

        // Act
        var users = await _repository.SearchUsersAsync("John");

        // Assert
        Assert.That(users.Count(), Is.EqualTo(1));
        Assert.That(users.First().Username, Is.EqualTo("JohnDoe"));
    }

    [Test]
    public async Task SearchUsersAsync_Should_FindUsersByEmail()
    {
        // Arrange
        await CreateTestUser("john@example.com", "hash", "JohnDoe");
        await CreateTestUser("jane@example.com", "hash", "JaneSmith");

        // Act
        var users = await _repository.SearchUsersAsync("jane@");

        // Assert
        Assert.That(users.Count(), Is.EqualTo(1));
        Assert.That(users.First().UserId, Is.EqualTo("jane@example.com"));
    }

    #endregion

    #region GetRecentUsersAsync Tests

    [Test]
    public async Task GetRecentUsersAsync_Should_ReturnMostRecentUsers()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(10); // Ensure different timestamps
            await CreateTestUser($"user{i}@example.com");
        }

        // Act
        var users = (await _repository.GetRecentUsersAsync(3)).ToList();

        // Assert
        Assert.That(users.Count, Is.EqualTo(3));
        Assert.That(users[0].UserId, Is.EqualTo("user5@example.com")); // Most recent
        Assert.That(users[1].UserId, Is.EqualTo("user4@example.com"));
        Assert.That(users[2].UserId, Is.EqualTo("user3@example.com"));
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUser(string userId, string password = "hashedpassword", string? username = null)
    {
        var user = new User
        {
            UserId = userId,
            Username = username ?? userId.Split('@')[0],
            Password = password,
            Role = "User"
        };
        return await _repository.CreateUserAsync(user);
    }

    private async Task SeedTestUsers()
    {
        var user1 = new User
        {
            UserId = "user1@example.com",
            Username = "user1",
            Password = "hash1",
            Role = "User"
        };

        var user2 = new User
        {
            UserId = "user2@example.com",
            Username = "user2",
            Password = "hash2",
            Role = "Admin"
        };

        var deletedUser = new User
        {
            UserId = "deleted@example.com",
            Username = "deleted",
            Password = "hash3",
            Role = "User",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user1);
        await _context.Users.AddAsync(user2);
        await _context.Users.AddAsync(deletedUser);
        await _context.SaveChangesAsync();
    }

    #endregion
}
