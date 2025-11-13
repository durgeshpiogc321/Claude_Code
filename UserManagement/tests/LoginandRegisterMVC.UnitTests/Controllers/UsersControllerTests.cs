using LoginandRegisterMVC.Controllers;
using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Repositories;
using LoginandRegisterMVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LoginandRegisterMVC.UnitTests.Controllers;

[TestFixture]
public class UsersControllerTests
{
    private UserContext _context = null!;
    private Mock<IUserService> _mockUserService = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<ISecurePasswordHashService> _mockSecurePasswordHashService = null!;
    private Mock<IWebHostEnvironment> _mockHostEnvironment = null!;
    private Mock<ILogger<UsersController>> _mockLogger = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UserContext(options);
        _mockUserService = new Mock<IUserService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSecurePasswordHashService = new Mock<ISecurePasswordHashService>();
        _mockHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<UsersController>>();

        // Setup WebRootPath for file uploads
        _mockHostEnvironment.Setup(m => m.WebRootPath).Returns("wwwroot");

        _controller = new UsersController(
            _mockUserService.Object,
            _mockUserRepository.Object,
            _mockSecurePasswordHashService.Object,
            _mockHostEnvironment.Object,
            _mockLogger.Object);

        // Setup HttpContext for session
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _controller.Dispose();
    }

    [Test]
    public void Register_Get_ReturnsViewWithViewModel()
    {
        // Act
        var result = _controller.Register() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.InstanceOf<LoginandRegisterMVC.ViewModels.UserRegistrationViewModel>());
    }

    [Test]
    public async Task Login_Get_ReturnsViewWithViewModel()
    {
        // Arrange
        _mockUserService.Setup(x => x.UserExistsAsync("admin@demo.com"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Login() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.InstanceOf<LoginandRegisterMVC.ViewModels.UserLoginViewModel>());
    }

    [Test]
    public async Task Index_ReturnsViewWithUsers_WhenAuthorized()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                UserId = "test@test.com",
                Username = "testuser",
                Password = "hashedpassword",
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var userListViewModel = new LoginandRegisterMVC.ViewModels.UserListPageViewModel
        {
            Users = users,
            TotalUsers = 1,
            CurrentPage = 1,
            PageSize = 10,
            SearchTerm = null,
            RoleFilter = null,
            ActiveFilter = null,
            SortBy = "CreatedAt",
            SortOrder = "desc"
        };

        _mockUserService.Setup(x => x.GetFilteredUsersAsync(
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()))
            .ReturnsAsync(userListViewModel);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var model = result.Model as IEnumerable<User>;
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Count(), Is.EqualTo(1));
    }
}
