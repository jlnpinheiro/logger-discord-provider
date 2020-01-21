using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace JNogueira.Logger.Discord
{
    public class DiscordLoggerProvider : ILoggerProvider
    {
        private readonly IHttpContextAccessor _httpContextAcessor;
        private readonly DiscordLoggerOptions _options;

        public DiscordLoggerProvider(DiscordLoggerOptions options, IHttpContextAccessor httpContextAcessor)
        {
            _options            = options;
            _httpContextAcessor = httpContextAcessor;
        }

        public ILogger CreateLogger(string name)
        {
            return new DiscordLogger()
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
