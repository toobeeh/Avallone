using Microsoft.AspNetCore.Authorization;
using tobeh.Avallone.Server.Classes;
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
        
        var claimReceivedTimestamp = DateTimeOffset.UtcNow;

        AnnouncedDropDetails dropAnnouncement;
        try
        {
            dropAnnouncement = RsaHelper.ParseDropToken(dropClaim.DropToken);
        }
        catch (Exception e)
        {
            throw new ForbiddenException("Failed to validate drop claim");
        }
        
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
        
        /* get league mode*/
        var leagueMode = lobby.Players.Count == 1 || lobby.Players.TrueForAll(p => p.Score == 0);
        
        /* claim drop in valmar */
        var claimResult = await dropsClient.ClaimDropAsync(new ClaimDropMessage
        {
            DropId = dropAnnouncement.DropId, 
            LeagueMode = leagueMode
        });
        var resultNotification = new DropClaimResultDto(username, claimResult.FirstClaim, claimResult.ClearedDrop,
            claimResult.CatchMs, claimResult.LeagueWeight, dropAnnouncement.DropId, claimResult.LeagueMode);
        
        var realDelay = dropAnnouncement.AnnouncementTimestamp - claimReceivedTimestamp;
        logger.LogInformation("Received drop claim for {username} in {realDelay}ms, logged as {delay}", username, realDelay, claimResult.CatchMs);
        
        /* log drop */
        dropsClient.LogDropClaimAsync(new LogDropMessage
        {
            CatchMs = claimResult.CatchMs,
            ClaimTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            DiscordId = discordId,
            DropId = dropAnnouncement.DropId,
            EventDropId = claimResult.EventDropId,
            LobbyKey = lobbyContext.OwnerClaim.LobbyId
        });
        
        /* reward drop only if not in league mode */
        if(!leagueMode)
        {
            var value = claimResult.FirstClaim ? Math.Max(1, claimResult.LeagueWeight) : claimResult.LeagueWeight;
            dropsClient.RewardDropAsync(new RewardDropMessage
            {
                EventDropId = claimResult.EventDropId,
                Login = lobbyContext.PlayerLogin,
                Value = value
            });
        }

        /* notify others, and return result for own */
        await Clients.AllExcept([Context.ConnectionId]).DropClaimed(resultNotification);
        return resultNotification;
    }
}