using System.Security.Claims;
using tobeh.Avallone.Server.Authentication;

namespace tobeh.Avallone.Server.Util;

public class TypoTokenHandlerHelper
{
    public static List<Claim> CreateServerConnectionClaims(IEnumerable<long> serverConnections)
    {
        return serverConnections
            .Select(invite => new Claim(TypoTokenDefaults.GuildClaimName, invite.ToString()))
            .ToList();
    }
    
    public static List<long> ExtractServerConnectionClaims(IEnumerable<Claim> claims)
    {
        return claims
            .Where(claim => claim.Type == TypoTokenDefaults.GuildClaimName)
            .Select(claim => long.Parse(claim.Value))
            .ToList();
    }
    
    public static int ExtractLoginClaim(IEnumerable<Claim> claims)
    {
        return int.Parse(claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value ?? throw new NullReferenceException("No id claim present"));
    }
}