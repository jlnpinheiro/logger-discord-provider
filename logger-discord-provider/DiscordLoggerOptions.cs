using System;
using System.Collections.Generic;

namespace JNogueira.Logger.Discord
{
    public class DiscordLoggerOptions
    {
        public string WebhookUrl { get; private set; }

        public string ApplicationName { get; set; }

        public string UserName { get; set; }

        public string EnvironmentName { get; set; }

        public List<UserClaimValueToDiscordField> UserClaimValueToDiscordFields { get; set; } = new List<UserClaimValueToDiscordField>();

        public DiscordLoggerOptions(string webhookUrl)
        {
            if (string.IsNullOrEmpty(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl), "The Discord webhook URL cannot be null or empty.");

            if (!(Uri.TryCreate(webhookUrl, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == "http" || uriResult.Scheme == "https")))
            {
                throw new ArgumentException($"Invalid Discord webhook URL: {webhookUrl}");
            }

            this.WebhookUrl  = webhookUrl;
        }
    }
}
