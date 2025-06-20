using JNogueira.Discord.WebhookClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace JNogueira.Discord.Logger;

public class DiscordLoggerProvider(string discordWebhookUrl, DiscordLoggerConfiguration config, IHttpClientFactory httpClientFactory) : ILoggerProvider
{
    public ILogger CreateLogger(string name) => new DiscordLogger(discordWebhookUrl, config, httpClientFactory);

    public void Dispose()
    {}
}

public static class DiscordLoggerProviderExtensions
{
    public static ILoggingBuilder AddDiscordLogger(this ILoggingBuilder builder, string discordWebhookUrl, Action<DiscordLoggerConfiguration> configure = null)
    {
        ArgumentNullException.ThrowIfNull(discordWebhookUrl);
        
        if (!IsValidDiscordWebhookUrl(discordWebhookUrl))
            throw new ArgumentException("Invalid Discord webhook URL. Try with a URL like https://discord.com/api/webhooks...", nameof(discordWebhookUrl));

        builder.Services.AddHttpClient();

        var config = new DiscordLoggerConfiguration();
        configure?.Invoke(config);

        builder.Services.AddSingleton(config);

        builder.Services.AddSingleton<ILoggerProvider, DiscordLoggerProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return new DiscordLoggerProvider(discordWebhookUrl, config, httpClientFactory);
        });

        builder.AddFilter($"System.Net.Http.HttpClient.{nameof(DiscordWebhookHttpClient)}", LogLevel.Warning);

        return builder;
    }

    private static bool IsValidDiscordWebhookUrl(string discordWebhookUrl)
    {
        if (string.IsNullOrWhiteSpace(discordWebhookUrl))
            return false;

        try
        {
            var uri = new Uri(discordWebhookUrl);

            return uri.Scheme == "https" && uri.Host == "discord.com" && uri.AbsolutePath.StartsWith("/api/webhooks", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
