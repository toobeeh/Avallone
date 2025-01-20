using Microsoft.AspNetCore.Authorization;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Hubs;

public partial class LobbyHub
{
    [Authorize]
    public async Task<DropClaimResultDto> ClaimDrop(DropClaimDto dropClaim)
    {
        logger.LogTrace("ClaimDrop(dropClaim={dropClaim})", dropClaim);
        
        var discordId = TypoTokenHandlerHelper.ExtractDiscordIdClaim(Context.User?.Claims ?? []);
        var dropBan = TypoTokenHandlerHelper.HasDropBanClaim(Context.User?.Claims ?? []);
        if (dropBan)
        {
            throw new ForbiddenException("User id drop banned");
        }
        
        /* get username */
        var lobbyContext = lobbyContextStore.RetrieveContextFromClient(Context.ConnectionId);
        var lobby = lobbyService.GetSkribblLobbyState(lobbyContext);
        var username = lobby.Players.FirstOrDefault(player => player.PlayerId == lobbyContext.PlayerId)?.Name;
        if(username is null)
        {
            throw new EntityNotFoundException("Could not find player in lobby");
        }
        
        /* claim drop in valmar */
        var claimResult = await dropsClient.ClaimDropAsync(new ClaimDropMessage { DropId = dropClaim.DropId });
        var resultNotification = new DropClaimResultDto(username, claimResult.FirstClaim, claimResult.ClearedDrop,
            claimResult.CatchMs, claimResult.LeagueWeight, dropClaim.DropId, false);
        
        /* log drop */
        dropsClient.LogDropClaimAsync(new LogDropMessage
        {
            CatchMs = claimResult.CatchMs,
            ClaimTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            DiscordId = discordId,
            DropId = dropClaim.DropId,
            EventDropId = claimResult.EventDropId,
            LobbyKey = lobbyContext.OwnerClaim.LobbyId,
        });
        
        /* check if drop was caught in league grind mode */
        if (lobby.Players.Count == 1 || lobby.Players.TrueForAll(p => p.Score == 0))
        {
            resultNotification = resultNotification with { leagueMode = true };
        }
        else
        {
            /* reward drop */
            dropsClient.RewardDropAsync(new RewardDropMessage
            {
                EventDropId = claimResult.EventDropId,
                Login = lobbyContext.PlayerLogin,
                Value = claimResult.LeagueWeight
            });
        }

        /* notify others, and return result for own */
        await Clients.AllExcept([Context.ConnectionId]).DropClaimed(resultNotification);
        return resultNotification;
    }
}