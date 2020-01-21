using System;

namespace JNogueira.Logger.Discord
{
    public class DiscordLoggerOptions
    {
        public string WebhookUrl { get; private set; }

        public string ChannelName { get; private set; }

        public string ApplicationName { get; set; }

        public string UserName { get; set; }

        public string EnvironmentName { get; set; }

        public DiscordLoggerOptions(string webhookUrl, string channelName)
        {
            if (string.IsNullOrEmpty(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            Uri uriResult;
            if (!(Uri.TryCreate(webhookUrl, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == "http" || uriResult.Scheme == "https")))
            {
                throw new ArgumentException($"Invalid webhook URL: {webhookUrl}");
            }

            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentNullException(nameof(channelName));

            this.WebhookUrl  = webhookUrl;
            this.ChannelName = channelName;
        }
    }
}
