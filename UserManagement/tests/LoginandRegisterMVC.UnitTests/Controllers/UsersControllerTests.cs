using LoginandRegisterMVC.Controllers;
using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Services;
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
    private Mock<ISecurePasswordHashService> _mockSecurePasswordHashService = null!;
#pragma warning disable CS0618
    private Mock<IPasswordHashService> _mockLegacyPasswordHashService = null!;
#pragma warning restore CS0618
    private Mock<ILogger<UsersController>> _mockLogger = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UserContext(options);
        _mockSecurePasswordHashService = new Mock<ISecurePasswordHashService>();
#pragma warning disable CS0618
        _mockLegacyPasswordHashService = new Mock<IPasswordHashService>();
#pragma warning restore CS0618
        _mockLogger = new Mock<ILogger<UsersController>>();

        _controller = new UsersController(
            _context,
            _mockSecurePasswordHashService.Object,
            _mockLegacyPasswordHashService.Object,
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
    public void Register_Get_ReturnsViewWithNewUser()
    {
        // Act
        var result = _controller.Register() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.InstanceOf<User>());
    }

    [Test]
    public async Task Login_Get_CreatesAdminUser_WhenNotExists()
    {
        // Arrange
        _mockSecurePasswordHashService.Setup(x => x.HashPassword("Admin@123"))
            .Returns("hashedpassword");

        // Act
        var result = await _controller.Login() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == "admin@demo.com");
        Assert.That(adminUser, Is.Not.Null);
        Assert.That(adminUser.Username, Is.EqualTo("admin"));
        Assert.That(adminUser.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task Index_ReturnsViewWithUsers_WhenAuthorized()
    {
        // Arrange
        var user = new User
        {
            UserId = "test@test.com",
            Username = "testuser",
            Password = "hashedpassword",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var model = result.Model as IEnumerable<User>;
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Count(), Is.EqualTo(1));
    }
}
