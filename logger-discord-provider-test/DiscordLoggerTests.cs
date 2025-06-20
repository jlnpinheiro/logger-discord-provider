using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace JNogueira.Discord.Logger.Test;

public class DiscordLoggerTests
{
    protected ServiceProvider _serviceProvider;
    protected IServiceCollection _serviceCollection;
    protected ILogger<DiscordLoggerTests> _logger;

    [SetUp]
    public void SetupTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("testSettings.json")
            .Build();

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new (ClaimTypes.NameIdentifier, "SomeValueHere"),
                new (ClaimTypes.Name, "gunnar@somecompany.com")
            ])
        );

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(new DefaultHttpContext { User = user });

        var _serviceCollection = new ServiceCollection()
            .AddLogging(c =>
            {
                c.AddDiscordLogger(configuration["DiscordWebhookUrl"], config =>
                {
                    config.ApplicationName = "Test Application";
                    config.EnvironmentName = "Test Environment";
                    config.UserName = "TestUserName";
                    config.HttpContextAccessor = mockHttpContextAccessor.Object;
                    config.UserClaimValueToDiscordFields = [new(ClaimTypes.NameIdentifier, "Name identifier"), new(ClaimTypes.Name, "Name")];
                });
                c.SetMinimumLevel(LogLevel.Trace);
            });

        var _serviceProvider = _serviceCollection.BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<DiscordLoggerTests>>();
    }

    [Test]
    public void Should_Send_A_Discord_Trace_Message()
    {
        _logger.LogTrace("My trace message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Discord_Debug_Message()
    {
        _logger.LogDebug("My debug message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Discord_Information_Message()
    {
        _logger.LogInformation("My info message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Discord_Warning_Message()
    {
        _logger.LogWarning("My warning message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Discord_Error_Message()
    {
        _logger.LogError("My error message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Discord_Critical_Message()
    {
        _logger.LogCritical("My critical message is here!");

        Assert.Pass();
    }

    [Test]
    public void Should_Send_A_Exception_Message()
    {
        try
        {
            var i = 0;

            var x = 5 / i; // <== Force an exception to be logged.
        }
        catch (Exception ex)
        {
            ex.Data["Extra info 1"] = "Extra info 1 value"; // Custom exception info sample.
            ex.Data["Extra info 2"] = "Extra info 2 value";

            _logger.LogError(ex, "A exception is handled!");

            Assert.Pass();
        }
    }

    [Test]
    public void Should_Send_A_Message_As_Attachment_On_Exception()
    {
        _logger.LogInformation(new string('0', 2300));
    }

    [TearDown]
    public void DisposeTest()
    {
        _serviceProvider?.Dispose();
        _serviceCollection = null;
        _serviceProvider = null;
        _logger = null;
    }
}
