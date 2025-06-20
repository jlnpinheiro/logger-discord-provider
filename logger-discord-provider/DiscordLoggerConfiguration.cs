using JNogueira.Logger.Discord;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JNogueira.Discord.Logger;

public sealed class DiscordLoggerConfiguration
{
    public string ApplicationName { get; set; }
    public string UserName { get; set; }
    public string EnvironmentName { get; set; }
    public IHttpContextAccessor HttpContextAccessor { get; set; }
    public List<UserClaimValueToDiscordField> UserClaimValueToDiscordFields { get; set; } = [];
}
