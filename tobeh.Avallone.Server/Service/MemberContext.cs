using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Service;

public class MemberContext(
    ILogger<MemberContext> logger, 
    IHttpContextAccessor httpContextAccessor,
    Members.MembersClient membersClient,
    MemberContextCache memberContextCache
    )
{
    public async Task<MemberReply> GetMember()
    {
        logger.LogTrace("GetMember()");
        
        var subClaim = httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (subClaim == null)
        {
            logger.LogError("User sub claim not found");
            throw new UnauthorizedAccessException("User not found");
        }

        var cachedMember = memberContextCache.GetCachedMemberForId(subClaim.Value);
        if (cachedMember != null)
        {
            logger.LogDebug("Returning cached member for sub={sub}", subClaim.Value);
            return cachedMember;
        }
        
        var login = int.Parse(subClaim.Value);
        var member = await membersClient.GetMemberByLoginAsync(new IdentifyMemberByLoginRequest { Login = login });
        
        memberContextCache.CacheMemberForId(subClaim.Value, member);

        return member;
    }
}