using JNogueira.Discord.Webhook.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JNogueira.Logger.Discord
{
    public class DiscordLogger : ILogger
    {
        private readonly DiscordLoggerOptions _options;
        private readonly IHttpContextAccessor _httpContextAcessor;

        public DiscordLogger(DiscordLoggerOptions options, IHttpContextAccessor httpContextAcessor)
        {
            _options            = options;
            _httpContextAcessor = httpContextAcessor;
        }
        
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var client = new DiscordWebhookClient(_options.WebhookUrl);

            DiscordMessage message = null;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var formattedMessage = formatter(state, exception);

            if (string.IsNullOrEmpty(formattedMessage))
                return;

            var messageContent = string.Empty;
            int? messageEmbedColor = null;

            switch (logLevel)
            {
                case LogLevel.None:
                case LogLevel.Trace:
                    messageContent = $"**{logLevel}**: {formattedMessage}";
                    break;
                case LogLevel.Debug:
                    messageContent = $"{DiscordEmoji.SpiderWeb} **{logLevel}**: {formattedMessage}";
                    break;
                case LogLevel.Information:
                    messageContent = $"{DiscordEmoji.InformationSource} **{logLevel}**: {formattedMessage}";
                    messageEmbedColor = (int)DiscordColor.Blue;
                    break;
                case LogLevel.Warning:
                    messageContent = $"{DiscordEmoji.Warning} **{logLevel}**: {formattedMessage}";
                    messageEmbedColor = (int)DiscordColor.Yellow;
                    break;
                case LogLevel.Critical:
                    messageContent = $"{DiscordEmoji.Radioactive} **{logLevel}**: {formattedMessage}";
                    messageEmbedColor = 16711680;
                    break;
                case LogLevel.Error:
                    messageContent = $"{DiscordEmoji.Skull} **{logLevel}**: {formattedMessage}";
                    messageEmbedColor = (int)DiscordColor.Red;
                    break;
            }

            var fields = new List<DiscordMessageEmbedField>();

            if (!string.IsNullOrEmpty(_options.ApplicationName))
                fields.Add(new DiscordMessageEmbedField("Application name", _options.ApplicationName));

            if (!string.IsNullOrEmpty(_options.EnvironmentName))
                fields.Add(new DiscordMessageEmbedField("Environment name", _options.EnvironmentName));

            if (_httpContextAcessor?.HttpContext?.User != null && _options.UserClaimValueToDiscordFields.Count > 0)
            {
                foreach(var item in _options.UserClaimValueToDiscordFields.Where(x => !string.IsNullOrEmpty(x.DiscordFieldName)))
                {
                    var claimValue = _httpContextAcessor?.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == item.ClaimType)?.Value;
                    fields.Add(new DiscordMessageEmbedField(item.DiscordFieldName, claimValue));
                }
            }

            var files = new List<DiscordFile>();

            DiscordMessageEmbed embed = null;

            if (exception != null)
            {
                fields.Add(new DiscordMessageEmbedField("Exception type", exception.GetType().ToString()));
                fields.Add(new DiscordMessageEmbedField("Source", exception.Source));

                var exceptionInfoText = new StringBuilder();

                exceptionInfoText.Append("Message: ").AppendLine(exception.Message);
                exceptionInfoText.Append("Exception type: ").AppendLine(exception.GetType().ToString());
                exceptionInfoText.Append("Source: ").AppendLine(exception.Source);
                exceptionInfoText.Append("Base exception: ").AppendLine(exception.GetBaseException()?.Message);

                foreach (DictionaryEntry data in exception.Data)
                    exceptionInfoText.Append(data.Key).Append(": ").Append(data.Value).AppendLine();

                exceptionInfoText.Append("Stack trace: ").AppendLine(exception.StackTrace);

                if (_httpContextAcessor?.HttpContext?.Request != null)
                {
                    var uriBuilder = new UriBuilder
                    {
                        Scheme = _httpContextAcessor.HttpContext.Request.Scheme,
                        Host   = _httpContextAcessor.HttpContext.Request.Host.Host,
                        Path   = _httpContextAcessor.HttpContext.Request.Path.ToString(),
                        Query  = _httpContextAcessor.HttpContext.Request.QueryString.ToString()
                    };

                    if (_httpContextAcessor.HttpContext.Request.Host.Port.HasValue && _httpContextAcessor.HttpContext.Request.Host.Port.Value != 80)
                        uriBuilder.Port = _httpContextAcessor.HttpContext.Request.Host.Port.Value;

                    if (!string.IsNullOrEmpty(uriBuilder.Host))
                        fields.Add(new DiscordMessageEmbedField("URL", uriBuilder.Uri.ToString()));

                    var requestHeaders = new Dictionary<string, string>();

                    foreach (var item in _httpContextAcessor.HttpContext.Request.Headers?.Where(x => x.Key != "Cookie" && x.Value.Count > 0))
                        requestHeaders.Add(item.Key, string.Join(",", item.Value.ToArray()));

                    if (requestHeaders.Count > 0)
                        exceptionInfoText.AppendLine("Request headers:").AppendLine(JsonConvert.SerializeObject(requestHeaders, Formatting.Indented));
                }

                files.Add(new DiscordFile("exception-details.txt", Encoding.UTF8.GetBytes(exceptionInfoText.ToString())));

                embed = new DiscordMessageEmbed(color: messageEmbedColor, description: $"**{exception.Message}**", fields: fields.Count > 0 ? fields.ToArray() : null);
            }
            else if (fields.Count > 0)
            {
                embed = new DiscordMessageEmbed(color: messageEmbedColor, fields: fields.ToArray());
            }

            message = new DiscordMessage(messageContent, _options.UserName, embeds: embed != null ? new[] { embed } : null);

            if (files.Count > 0)
            {
                client.SendToDiscord(message, files.ToArray(), true).Wait();
            }
            else
            {
                client.SendToDiscord(message, true).Wait();
            }
        }
    }
}
