using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using tobeh.Avallone.Server.Authentication;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Hubs;

public class LobbyHub(
    ILogger<LobbyHub> logger,
    LobbyContextStore lobbyContextStore,
    LobbyService lobbyService
    ) : Hub<ILobbyReceiver>, ILobbyHub
{
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogTrace("OnDisconnectedAsync(exception={exception})", exception);
        logger.LogDebug("Client disconnected: {id}", Context.ConnectionId);
        
        var id = Context.ConnectionId;
        var context = lobbyContextStore.RetrieveContextFromClient(id);
        
        // remove context in store
        await lobbyContextStore.DetachContextFromClient(id);
        await Groups.RemoveFromGroupAsync(context.OwnerClaim.LobbyId, id);
        
        // if client was owner, request new ownership claims from other clients
        var ownershipRemoved = await lobbyService.TryRemoveOwnershipFromLobby(context);
        if (ownershipRemoved)
        {
            logger.LogDebug("Owner disconnected, requesting new ownership claims");
            await Clients.Group(context.OwnerClaim.LobbyId).LobbyOwnershipResigned();
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    [Authorize]
    public async Task<TypoLobbyStateDto> LobbyDiscovered(LobbyDiscoveredDto lobbyDiscovery)
    {
        logger.LogTrace("LobbyDiscovered(lobbyDiscovery={lobbyDiscovery})", lobbyDiscovery);

        var login = TypoTokenHandlerHelper.ExtractLoginClaim(Context.User?.Claims ?? []);
        var serverConnections = TypoTokenHandlerHelper.ExtractServerConnectionClaims(Context.User?.Claims ?? []);
        var context = lobbyContextStore.AttachContextToClient(Context.ConnectionId, lobbyDiscovery.Lobby.Link, lobbyDiscovery.PlayerId, login, serverConnections, lobbyDiscovery.OwnerClaimToken);
        
        // try to (re)claim ownership with existing token, eg when disconnected from lobby temporarily, or when server restarted
        var claimResult = await lobbyService.ClaimLobbyOwnership(context);
        logger.LogDebug("New lobby context attached to lobby: {context}, {state}", context, claimResult.State);
        
        // if claim successful, notify other clients in group
        if (claimResult.ClaimSuccessful)
        {
            logger.LogDebug("Connection claim successful, notifying group");
            await Clients.Group(context.OwnerClaim.LobbyId).TypoLobbySettingsUpdated(claimResult.State.LobbySettings);
        }
        
        // save reported current skribbl state
        lobbyService.SaveSkribblLobbyState(context, lobbyDiscovery.Lobby);

        // add client to lobby group
        await Groups.AddToGroupAsync(Context.ConnectionId, context.OwnerClaim.LobbyId);
        
        if(Context.ConnectionAborted.IsCancellationRequested)
        {
            logger.LogDebug("Connection aborted during setup, executing disconnect handler");
            await OnDisconnectedAsync(null);
        }
        
        return claimResult.State;
    }

    [Authorize]
    public async Task ClaimLobbyOwnership()
    {
        logger.LogTrace("ClaimLobbyOwnership()");
        
        var context = lobbyContextStore.RetrieveContextFromClient(Context.ConnectionId);
        var claimResult = await lobbyService.ClaimLobbyOwnership(context);
        
        // if claim successful, notify clients in group
        logger.LogDebug("Ownership claimed: {claimResult} by {id}", claimResult.ClaimSuccessful, context.PlayerId);
        if (claimResult.ClaimSuccessful)
        {
            logger.LogDebug("Claim successful, notifying group");
            await Clients.Group(context.OwnerClaim.LobbyId).TypoLobbySettingsUpdated(claimResult.State.LobbySettings);
        }
    }
    
    [Authorize]
    public Task UpdateSkribblLobbyState(SkribblLobbyStateDto state)
    {
        logger.LogTrace("UpdateSkribblLobbyState(state={state})", state);
        
        var context = lobbyContextStore.RetrieveContextFromClient(Context.ConnectionId);
        lobbyService.SaveSkribblLobbyState(context, state);
        return Task.CompletedTask;
    }

    [Authorize]
    public async Task UpdateTypoLobbySettings(SkribblLobbyTypoSettingsUpdateDto typoSettings)
    {
        logger.LogTrace("UpdateTypoLobbySettings(typoSettings={typoSettings})", typoSettings);
        
        var context = lobbyContextStore.RetrieveContextFromClient(Context.ConnectionId);
        var settings = await lobbyService.UpdateTypoLobbySettings(context, typoSettings);
        await Clients.Group(context.OwnerClaim.LobbyId).TypoLobbySettingsUpdated(settings);
        logger.LogDebug("Updated lobby settings: {settings}", settings);
    }
}