﻿using JNogueira.Discord.Webhook.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var formattedMessage = formatter(state, exception);

            if (string.IsNullOrEmpty(formattedMessage))
                return;

            DiscordMessage message = null;

            DiscordMessageEmbed embed = null;

            switch (logLevel)
            {
                case LogLevel.None:
                case LogLevel.Trace:
                    message = new DiscordMessage(logLevel.ToString(), _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage);
                    break;
                case LogLevel.Debug:
                    message = new DiscordMessage($"{DiscordEmoji.SpiderWeb} {logLevel.ToString()}", _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage);
                    break;
                case LogLevel.Information:
                    message = new DiscordMessage($"{DiscordEmoji.InformationSource} {logLevel.ToString()}", _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage, 31743);
                    break;
                case LogLevel.Warning:
                    message = new DiscordMessage($"{DiscordEmoji.Warning} {logLevel.ToString()}", _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage, 16761095);
                    break;
                case LogLevel.Critical:
                    message = new DiscordMessage($"{DiscordEmoji.Radioactive} {logLevel.ToString()}", _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage, 16711680);
                    break;
                case LogLevel.Error:
                    message = new DiscordMessage($"{DiscordEmoji.Skull} {logLevel.ToString()}", _options.UserName);
                    embed = new DiscordMessageEmbed(formattedMessage, 14431557);
                    break;
            }

            var fields = new List<DiscordMessageEmbedField>();

            if (!string.IsNullOrEmpty(_options.ApplicationName))
                fields.Add(new DiscordMessageEmbedField("Application name", _options.ApplicationName));

            if (!string.IsNullOrEmpty(_options.EnvironmentName))
                fields.Add(new DiscordMessageEmbedField("Environment name", _options.EnvironmentName));

            if (exception != null)
            {
                embed.Title = exception.Message;
                
                embed.Description = formattedMessage;
                
                if (!string.IsNullOrEmpty(exception.StackTrace))
                    fields.Add(new DiscordMessageEmbedField("Stack trace", $"`{exception.StackTrace}`"));

                if (!string.IsNullOrEmpty(exception.Source))
                    fields.Add(new DiscordMessageEmbedField("Source", exception.Source));

                if (exception.GetBaseException()?.Message != exception.Message)
                    fields.Add(new DiscordMessageEmbedField("Base exception", exception.GetBaseException()?.Message));

                foreach (DictionaryEntry data in exception.Data)
                    fields.Add(new DiscordMessageEmbedField(data.Key.ToString(), data.Value.ToString()));

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
                        fields.Add(new DiscordMessageEmbedField("Request header", JsonConvert.SerializeObject(requestHeaders, Formatting.Indented)));
                }
            }

            embed.Fields = fields.ToArray();

            message.Embeds = new[] { embed };

            var client = new DiscordWebhookClient(_options.WebhookUrl);

            client.SendToDiscord(message).Wait();
        }
    }
}
