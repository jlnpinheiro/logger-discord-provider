namespace JNogueira.Logger.Discord
{
    public class UserClaimValueToDiscordField
    {
        public string ClaimType { get; }

        public string DiscordFieldName { get; }

        public UserClaimValueToDiscordField(string claimType, string discordFieldName)
        {
            ClaimType = claimType?.Trim();
            DiscordFieldName = discordFieldName?.Trim();
        }
    }
}
