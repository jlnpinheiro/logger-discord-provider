using JNogueira.Logger.Discord;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace logger_discord_provider_tests
{
    [TestClass]
    public class DiscordLoggerTests
    {
        private ILogger _logger;

        public DiscordLoggerTests()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("testSettings.json")
               .Build();

            _logger = GetDiscordLogger(config["DiscordWebhookUrl"]);
        }

        private ILogger GetDiscordLogger(string urlWebhook)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.SetMinimumLevel(LogLevel.Trace))
                .BuildServiceProvider();

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "SomeValueHere"),
                    new Claim(ClaimTypes.Name, "gunnar@somecompany.com")
                })
            );

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(new DefaultHttpContext { User = user });

            var factory = serviceProvider.GetService<ILoggerFactory>();
            factory.AddDiscord(new DiscordLoggerOptions(urlWebhook)
            {
                ApplicationName = "Application Name Test",
                EnvironmentName = "Name of environment",
                UserName = "discord-logger-test",
                UserClaimValueToDiscordFields = new List<UserClaimValueToDiscordField> { new UserClaimValueToDiscordField(ClaimTypes.NameIdentifier, "Name identifier"), new UserClaimValueToDiscordField(ClaimTypes.Name, "Name") }
            }, mockHttpContextAccessor.Object);

            return factory.CreateLogger<DiscordLoggerTests>();
        }

        [TestMethod]
        public void Should_Send_A_Discord_Trace_Message()
        {
            _logger.LogTrace("My trace message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Debug_Message()
        {
            _logger.LogDebug("My debug message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Information_Message()
        {
            _logger.LogInformation("My info message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Warning_Message()
        {
            _logger.LogWarning("My warning message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Error_Message()
        {
            _logger.LogError("My error message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Critical_Message()
        {
            _logger.LogCritical("My critical message is here!");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Exception_Message()
        {
            try
            {
                var i = 0;

                var x = 5 / i;
            }
            catch (Exception ex)
            {
                ex.Data["Extra info 1"] = "Extra info 1 value";
                ex.Data["Extra info 2"] = "Extra info 2 value";

                _logger.LogError(ex, "A exception is handled!");

                System.Threading.Thread.Sleep(1000);

                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void Should_Send_A_Message_As_Attachment_On_Exception()
        {
            _logger.LogInformation(new string('0', 2300));
        }

        [TestMethod]
        public void Should_Not_Allow_Empty_Webhook_Url()
        {
            try
            {
                var logger = GetDiscordLogger(string.Empty);

                Assert.IsTrue(false);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.GetType() == typeof(ArgumentNullException), ex.Message);
            }
        }
    }
}
