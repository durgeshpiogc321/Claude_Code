using LoginandRegisterMVC.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Text.RegularExpressions;

namespace LoginandRegisterMVC.IntegrationTests.Controllers;

[TestFixture]
public class UsersControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UserContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<UserContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<UserContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<UsersControllerIntegrationTests>>();

                    db.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Don't follow redirects automatically
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Get_LoginPage_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Users/Login");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Does.Contain("text/html"));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("LOGIN"));
    }

    [Test]
    public async Task Get_RegisterPage_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Users/Register");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Does.Contain("text/html"));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("Register"));
    }

    [Test]
    public async Task Get_IndexPage_RedirectsToLogin_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/Users/Index");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.ToString(), Does.Contain("/Users/Login"));
    }

    [Test]
    public async Task Post_Login_WithInvalidCredentials_ReturnsLoginPageWithError()
    {
        // Arrange - Get the login page first to get anti-forgery token
        var loginPage = await _client.GetAsync("/Users/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var formData = new Dictionary<string, string>
        {
            {"Email", "invalid@email.com"},  // Changed from UserId to Email
            {"Password", "wrongpassword"},
            {"__RequestVerificationToken", antiForgeryToken}
        };
        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Users/Login", formContent);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("LOGIN"));
    }

    /// <summary>
    /// Helper method to extract anti-forgery token from HTML content
    /// </summary>
    private static string ExtractAntiForgeryToken(string htmlContent)
    {
        var match = Regex.Match(htmlContent, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
