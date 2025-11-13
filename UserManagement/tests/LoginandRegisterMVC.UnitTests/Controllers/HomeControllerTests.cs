using LoginandRegisterMVC.Controllers;
using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using LoginandRegisterMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace LoginandRegisterMVC.UnitTests.Controllers;

[TestFixture]
public class HomeControllerTests
{
    private HomeController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _controller = new HomeController();
    }

    [TearDown]
    public void TearDown()
    {
        // HomeController doesn't implement IDisposable, so no disposal needed
    }

    [Test]
    public void Index_ReturnsViewResult()
    {
        // Act
        var result = _controller.Index();

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void About_ReturnsViewResult_WithMessage()
    {
        // Act
        var result = _controller.About() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.ViewBag.Message, Is.EqualTo("Your application description page."));
    }

    [Test]
    public void Contact_ReturnsViewResult_WithMessage()
    {
        // Act
        var result = _controller.Contact() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.ViewBag.Message, Is.EqualTo("Your contact page."));
    }
}
