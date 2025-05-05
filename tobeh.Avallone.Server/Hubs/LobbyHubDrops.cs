using Grpc.Core;
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
        logger.LogInformation("Processing drop claim for token {dropToken}", dropClaim.DropToken);
        
        var claimReceivedTimestamp = DateTimeOffset.UtcNow;

        /* parse signed/encrypted announcement */
        AnnouncedDropDetails dropAnnouncement;
        try
        {
            dropAnnouncement = CryptoHelper.ParseDropToken(cryptoService, dropClaim.DropToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to parse drop token");
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
        
        /* set last claimed drop to current id to prevent double claiming - throws if last claim is same id */
        lobbyContextStore.MarkDropAsClaimed(Context.ConnectionId, dropAnnouncement.DropId);
        
        /* get league mode*/
        var leagueMode = lobby.Players.Count == 1 || lobby.Players.TrueForAll(p => p.Score == 0);
        
        /* claim drop in valmar */
        ClaimDropResultMessage claimResult; 
        try {
            claimResult = await dropsClient.ClaimDropAsync(new ClaimDropMessage
            {
                DropId = dropAnnouncement.DropId, 
                LeagueMode = leagueMode
            });
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.FailedPrecondition) throw new ForbiddenException(e.Status.Detail);
            throw;
        }
        
        var resultNotification = new DropClaimResultDto(username, claimResult.FirstClaim, claimResult.ClearedDrop,
            claimResult.CatchMs, claimResult.LeagueWeight, dropAnnouncement.DropId, claimResult.LeagueMode);
        
        var realDelay = (claimReceivedTimestamp - dropAnnouncement.AnnouncementTimestamp).TotalMilliseconds;
        logger.LogInformation("Received drop claim for {username} / {userid} in {realDelay}ms, calculated as {delay} with difference of {diff}", username, discordId, realDelay, claimResult.CatchMs, claimResult.CatchMs - realDelay);
        
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
            logger.LogInformation("Rewarding drop for {username} / {userid}", username, discordId);
            var value = claimResult.FirstClaim ? Math.Max(1, claimResult.LeagueWeight) : claimResult.LeagueWeight;
            dropsClient.RewardDropAsync(new RewardDropMessage
            {
                EventDropId = claimResult.EventDropId,
                Login = lobbyContext.PlayerLogin,
                Value = value
            });
        }
        else
        {
            logger.LogInformation("Drop was in league mode, not rewarding drop for {username} / {userid}", username, discordId);
        }

        /* notify others, and return result for own */
        await Clients.AllExcept([Context.ConnectionId]).DropClaimed(resultNotification);
        return resultNotification;
    }
}