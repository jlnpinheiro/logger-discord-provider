using JNogueira.Discord.WebhookClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace JNogueira.Discord.Logger;

public sealed class DiscordLogger(string discordWebhookUrl, DiscordLoggerConfiguration config, IHttpClientFactory httpClientFactory) : ILogger
{
    private DiscordWebhookClient _webhookClient;

    private static readonly JsonSerializerOptions _defaultJsonSerializerOptions = new() { WriteIndented = true };

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        ArgumentNullException.ThrowIfNull(formatter);

        var formattedMessage = formatter(state, exception);

        if (string.IsNullOrEmpty(formattedMessage))
            return;

        var messageContent = string.Empty;
        int? messageEmbedColor = null;

        DiscordMessage message = null;

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

        if (!string.IsNullOrEmpty(config.ApplicationName))
            fields.Add(new DiscordMessageEmbedField("Application name", config.ApplicationName));

        if (!string.IsNullOrEmpty(config.EnvironmentName))
            fields.Add(new DiscordMessageEmbedField("Environment name", config.EnvironmentName));

        if (config.HttpContextAccessor?.HttpContext?.User is not null && config.UserClaimValueToDiscordFields.Count > 0)
        {
            foreach(var item in config.UserClaimValueToDiscordFields.Where(x => !string.IsNullOrEmpty(x.DiscordFieldName)))
            {
                var claimValue = config.HttpContextAccessor.HttpContext.User?.Claims?.FirstOrDefault(x => x.Type == item.ClaimType)?.Value;

                fields.Add(new DiscordMessageEmbedField(item.DiscordFieldName, claimValue));
            }
        }

        var files = new List<DiscordFile>();

        DiscordMessageEmbed embed = null;

        if (exception is null)
        {
            if (fields.Count > 0)
            {
                embed = new DiscordMessageEmbed(color: messageEmbedColor, fields: [.. fields]);
            }
        }
        else
        {
            fields.Add(new DiscordMessageEmbedField("Exception type", exception.GetType().ToString()));

            if (exception.Source is not null)
            {
                fields.Add(new DiscordMessageEmbedField("Source", exception.Source));
            }

            var exceptionInfoText = new StringBuilder();

            exceptionInfoText.Append("Message: ").AppendLine(exception.Message);
            exceptionInfoText.Append("Exception type: ").AppendLine(exception.GetType().ToString());
            exceptionInfoText.Append("Source: ").AppendLine(exception.Source);
            exceptionInfoText.Append("Base exception: ").AppendLine(exception.GetBaseException()?.Message);

            foreach (DictionaryEntry data in exception.Data)
                exceptionInfoText.Append(data.Key).Append(": ").Append(data.Value).AppendLine();

            exceptionInfoText.Append("Stack trace: ").AppendLine(exception.StackTrace);

            if (config.HttpContextAccessor?.HttpContext?.Request != null)
            {
                var uriBuilder = new UriBuilder
                {
                    Scheme = config.HttpContextAccessor.HttpContext.Request.Scheme,
                    Host = config.HttpContextAccessor.HttpContext.Request.Host.Host,
                    Path = config.HttpContextAccessor.HttpContext.Request.Path.ToString(),
                    Query = config.HttpContextAccessor.HttpContext.Request.QueryString.ToString()
                };

                if (config.HttpContextAccessor.HttpContext.Request.Host.Port.HasValue && config.HttpContextAccessor.HttpContext.Request.Host.Port.Value != 80)
                    uriBuilder.Port = config.HttpContextAccessor.HttpContext.Request.Host.Port.Value;

                if (!string.IsNullOrEmpty(uriBuilder.Host))
                    fields.Add(new DiscordMessageEmbedField("URL", uriBuilder.Uri.ToString()));

                var requestHeaders = new Dictionary<string, string>();

                foreach (var item in config.HttpContextAccessor.HttpContext.Request.Headers?.Where(x => x.Key != "Cookie" && x.Value.Count > 0))
                    requestHeaders.Add(item.Key, string.Join(",", [.. item.Value]));

                if (requestHeaders.Count > 0)
                {
                    exceptionInfoText
                        .AppendLine("Request headers:")
                        .AppendLine(JsonSerializer.Serialize(requestHeaders, _defaultJsonSerializerOptions));
                }
            }

            files.Add(new DiscordFile("exception-details.txt", Encoding.UTF8.GetBytes(exceptionInfoText.ToString())));

            embed = new DiscordMessageEmbed(color: messageEmbedColor, description: $"**{exception.Message}**", fields: fields.Count > 0 ? fields.ToArray() : null);
        }

        message = new DiscordMessage(messageContent, config.UserName, embeds: embed != null ? [embed] : null);

        using var httpClient = httpClientFactory.CreateClient(nameof(DiscordWebhookHttpClient));

        httpClient.BaseAddress = new Uri(discordWebhookUrl);

        var discordClient = new DiscordWebhookHttpClient(httpClient);

        _webhookClient = new DiscordWebhookClient(discordClient);

        if (files.Count > 0)
        {
            _ = _webhookClient.SendToDiscordAsync(message, [.. files], true).GetAwaiter().GetResult();
        }
        else
        {
            _ = _webhookClient.SendToDiscordAsync(message, sendMessageAsFileAttachmentOnError: true).GetAwaiter().GetResult();
        }
    }
}