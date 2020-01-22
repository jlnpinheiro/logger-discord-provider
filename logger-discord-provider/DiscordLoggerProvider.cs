using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JNogueira.Logger.Discord
{
    public class DiscordLoggerProvider : ILoggerProvider
    {
        private readonly IHttpContextAccessor _httpContextAcessor;
        private readonly DiscordLoggerOptions _options;

        private DiscordLogger _logger;

        public DiscordLoggerProvider(DiscordLoggerOptions options, IHttpContextAccessor httpContextAcessor = null)
        {
            _options            = options;
            _httpContextAcessor = httpContextAcessor;
        }

        public ILogger CreateLogger(string name)
        {
            _logger = new DiscordLogger(_options, _httpContextAcessor);

            return _logger;
        }

        public void Dispose()
        {
            _logger = null;
        }
    }

    public static class DiscordLoggerProviderExtensions
    {
        public static ILoggerFactory AddDiscord(this ILoggerFactory loggerFactory, DiscordLoggerOptions options, IHttpContextAccessor httpContextAccessor = null)
        {
            loggerFactory.AddProvider(new DiscordLoggerProvider(options, httpContextAccessor));

            return loggerFactory;
        }
    }
}
