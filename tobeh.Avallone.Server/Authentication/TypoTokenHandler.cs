using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public class TypoTokenDefaults
{
    public const string AuthenticationScheme = "TypoToken";
    public const string GuildClaimName = "connected_guild_id";
    public const string DiscordIdClaimName = "typo_linked_discord_id";
    public const string DropBan = "typo_drops_ban";
}

public class TypoTokenHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    Members.MembersClient membersClient
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogTrace("HandleAuthenticateAsync()");
        
        // Get the token from the request
        var token = Request.Query["access_token"].FirstOrDefault() ?? Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            Logger.LogDebug("Authentication attempt without token");
            return AuthenticateResult.Fail("Token not found.");
        }

        // Validate the token (custom validation logic here)
        var claims = await ValidateToken(token);
        if (claims is null)
        {
            Logger.LogDebug("Authentication attempt with invalid token");
            return AuthenticateResult.Fail("Invalid token.");
        }

        // Create a ClaimsPrincipal
        var claimsIdentity = new ClaimsIdentity(claims, TypoTokenDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), TypoTokenDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<List<Claim>?> ValidateToken(string token)
    {
        Logger.LogTrace("ValidateToken(token={token})", token);
        var claims = new List<Claim>();

        try
        {
            var member = await membersClient.GetMemberByAccessTokenAsync(new IdentifyMemberByAccessTokenRequest { AccessToken = token });
            
            /* dont validate banned members */
            if (member.MappedFlags.Contains(MemberFlagMessage.PermaBan)) return null;
            
            claims.Add(new Claim(ClaimTypes.NameIdentifier, member.Login.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, member.Username));
            claims.Add(new Claim(TypoTokenDefaults.DiscordIdClaimName, member.DiscordId.ToString()));
            claims.AddRange(TypoTokenHandlerHelper.CreateServerConnectionClaims(member.ServerConnections));
            if(member.MappedFlags.Contains(MemberFlagMessage.DropBan)) claims.Add(new Claim(TypoTokenDefaults.DropBan, "true"));
            
            return claims;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
