using JNogueira.Logger.Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

            var factory = serviceProvider.GetService<ILoggerFactory>();
            factory.AddDiscord(new DiscordLoggerOptions(urlWebhook)
            {
                ApplicationName = "Application Name Test",
                EnvironmentName = "Name of environment",
                UserName = "discord-logger-test"
            });

            return factory.CreateLogger<DiscordLoggerTests>();
        }

        [TestMethod]
        public void Should_Send_A_Discord_Trace_Message()
        {
            _logger.LogTrace("OK! It's a trace message.");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Debug_Message()
        {
            _logger.LogDebug("OK! It's a debug message.");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Information_Message()
        {
            _logger.LogInformation("OK! It's a info message.");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Warning_Message()
        {
            _logger.LogWarning("OK! It's a warning message.");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Error_Message()
        {
            _logger.LogError("OK! It's a error message.");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Should_Send_A_Discord_Critical_Message()
        {
            _logger.LogCritical("OK! It's a critical message.");

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

                Assert.IsTrue(true);
            }
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
