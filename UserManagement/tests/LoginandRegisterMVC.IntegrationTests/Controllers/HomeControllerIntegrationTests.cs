using LoginandRegisterMVC.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;

namespace LoginandRegisterMVC.IntegrationTests.Controllers;

[TestFixture]
public class HomeControllerIntegrationTests
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
                    var logger = scopedServices.GetRequiredService<ILogger<HomeControllerIntegrationTests>>();

                    db.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Get_HomePage_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Home/Index");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Does.Contain("text/html"));
    }

    [Test]
    public async Task Get_AboutPage_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Home/About");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Does.Contain("text/html"));
    }

    [Test]
    public async Task Get_ContactPage_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Home/Contact");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Does.Contain("text/html"));
    }
}
