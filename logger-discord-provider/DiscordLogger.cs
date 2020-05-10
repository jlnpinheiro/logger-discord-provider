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

            try
            {
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
                        messageContent = logLevel.ToString();
                        break;
                    case LogLevel.Debug:
                        messageContent = $"{DiscordEmoji.SpiderWeb} **{logLevel.ToString()}**: {formattedMessage}";
                        break;
                    case LogLevel.Information:
                        messageContent = $"{DiscordEmoji.InformationSource} **{logLevel.ToString()}**: {formattedMessage}";
                        messageEmbedColor = 31743;
                        break;
                    case LogLevel.Warning:
                        messageContent = $"{DiscordEmoji.Warning} **{logLevel.ToString()}**: {formattedMessage}";
                        messageEmbedColor = 16761095;
                        break;
                    case LogLevel.Critical:
                        messageContent = $"{DiscordEmoji.Radioactive} **{logLevel.ToString()}**: {formattedMessage}";
                        messageEmbedColor = 16711680;
                        break;
                    case LogLevel.Error:
                        messageContent = $"{DiscordEmoji.Skull} **{logLevel.ToString()}**: {formattedMessage}";
                        messageEmbedColor = 14431557;
                        break;
                }

                var fields = new List<DiscordMessageEmbedField>();

                if (!string.IsNullOrEmpty(_options.ApplicationName))
                    fields.Add(new DiscordMessageEmbedField("Application name", _options.ApplicationName));

                if (!string.IsNullOrEmpty(_options.EnvironmentName))
                    fields.Add(new DiscordMessageEmbedField("Environment name", _options.EnvironmentName));

                var files = new List<DiscordFile>();

                DiscordMessageEmbed embed = null;

                if (exception != null)
                {
                    fields.Add(new DiscordMessageEmbedField("Exception type", exception.GetType().ToString()));
                    fields.Add(new DiscordMessageEmbedField("Source", exception.Source));

                    var exceptionInfoText = new StringBuilder();

                    exceptionInfoText.AppendFormat("Message: {0}\r\n", exception.Message);

                    exceptionInfoText.AppendFormat("Exception type: {0}\r\n", exception.GetType().ToString());

                    exceptionInfoText.AppendFormat("Source: {0}\r\n", exception.Source);

                    exceptionInfoText.AppendFormat("Base exception: {0}\r\n", exception.GetBaseException()?.Message);

                    foreach (DictionaryEntry data in exception.Data)
                        exceptionInfoText.Append($"{data.Key}: {data.Value}\r\n");

                    exceptionInfoText.AppendFormat("Stack trace: {0}\r\n", exception.StackTrace);

                    if (_httpContextAcessor != null)
                    {
                        var uriBuilder = new UriBuilder
                        {
                            Scheme = _httpContextAcessor.HttpContext?.Request?.Scheme,
                            Host = _httpContextAcessor.HttpContext?.Request?.Host.Host,
                            Path = _httpContextAcessor.HttpContext?.Request?.Path.ToString(),
                            Query = _httpContextAcessor.HttpContext?.Request?.QueryString.ToString()
                        };

                        if (_httpContextAcessor.HttpContext?.Request?.Host.Port.HasValue == true && _httpContextAcessor.HttpContext?.Request?.Host.Port.Value != 80)
                            uriBuilder.Port = _httpContextAcessor.HttpContext.Request.Host.Port.Value;

                        fields.Add(new DiscordMessageEmbedField("URL", uriBuilder.Uri.ToString()));

                        var requestHeaders = new Dictionary<string, string>();

                        foreach (var item in _httpContextAcessor.HttpContext?.Request?.Headers?.Where(x => x.Key != "Cookie" && x.Value.Count > 0))
                            requestHeaders.Add(item.Key, string.Join(",", item.Value.ToArray()));

                        if (requestHeaders.Count > 0)
                            exceptionInfoText.AppendFormat("Request headers: {0}\r\n", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestHeaders, Formatting.Indented)));
                    }

                    files.Add(new DiscordFile("exception-details.txt", Encoding.UTF8.GetBytes(exceptionInfoText.ToString())));

                    embed = new DiscordMessageEmbed(color: messageEmbedColor, description: $"**{exception.Message}**", fields: fields.ToArray());
                }
                else
                {
                    embed = new DiscordMessageEmbed(color: messageEmbedColor, fields: fields.ToArray());
                }

                var message = new DiscordMessage(messageContent, _options.UserName, embeds: new[] { embed });

                if (files.Count > 0)
                {
                    client.SendToDiscord(message, files.ToArray()).Wait();
                }
                else
                {
                    client.SendToDiscord(message).Wait();
                }
            }
            catch (Exception ex)
            {
                var message = new DiscordMessage($"{DiscordEmoji.Skull} **{logLevel.ToString()}**: {ex.GetBaseException().Message}", _options.UserName);

                client.SendToDiscord(message).Wait();
            }
        }
    }
}
