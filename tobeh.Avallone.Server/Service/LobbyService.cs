using KeyedSemaphores;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Service;

public class LobbyService(ILogger<LobbyService> logger, LobbyStore lobbyStore, Lobbies.LobbiesClient lobbiesClient)
{
    private async Task<TypoLobbyStateDto> GetTypoStateSettings(LobbyContext context)
    {
        logger.LogTrace("GetTypoStateSettings(context={context})", context);
        
        var lobby = await lobbiesClient.GetSkribblLobbyTypoSettingsAsync(new SkribblLobbyIdentificationMessage
            { Link = context.OwnerClaim.LobbyId });

        var ownerClaim = lobby.LobbyOwnershipClaim;
        var isOwner = context.OwnerClaim.Timestamp.ToUnixTimeMilliseconds() == ownerClaim;
        
        return new TypoLobbyStateDto(isOwner, context.OwnerClaim.Token, context.OwnerClaim.LobbyId, context.OwnerClaim.Timestamp.ToUnixTimeMilliseconds(),
            new TypoLobbySettingsDto(lobby.Description, lobby.WhitelistAllowedServers, 
                lobby.AllowedServers.Select(s => s.ToString()).ToList(), ownerClaim));
    }
    
    private async Task SetTypoStateSettings(LobbyContext context, TypoLobbySettingsDto settings)
    {
        logger.LogTrace("SetTypoStateSettings(settings={settings})", settings);
        
        await lobbiesClient.SetSkribblLobbyTypoSettingsAsync(new SkribblLobbyTypoSettingsMessage
        {
            LobbyId = context.OwnerClaim.LobbyId,
            Description = settings.Description,
            WhitelistAllowedServers = settings.WhitelistAllowedServers,
            AllowedServers = { settings.AllowedServers.Select(long.Parse) },
            LobbyOwnershipClaim = settings.LobbyOwnershipClaim
        });
    }

    public async Task<bool> TryRemoveOwnershipFromLobby(LobbyContext context)
    {
        logger.LogTrace("TryRemoveOwnershipFromLobby(context={context})", context);

        var state = await GetTypoStateSettings(context);
        if (!state.PlayerIsOwner) return false;
        
        using (await KeyedSemaphore.LockAsync(context.OwnerClaim.LobbyId))
        {
            await SetTypoStateSettings(context, state.LobbySettings with { LobbyOwnershipClaim = null });
        }

        return true;
    }

    public async Task<LobbyOwnerClaimResult> ClaimLobbyOwnership(LobbyContext context)
    {
        logger.LogTrace("ClaimLobbyOwnership(context={context})", context);

        using (await KeyedSemaphore.LockAsync(context.OwnerClaim.LobbyId))
        {
            var state = await GetTypoStateSettings(context);
            if (state.LobbySettings.LobbyOwnershipClaim is not null &&
                state.LobbySettings.LobbyOwnershipClaim < context.OwnerClaim.Timestamp.ToUnixTimeMilliseconds()) return new LobbyOwnerClaimResult(false, state);
        
            state = state with
            {
                PlayerIsOwner = true,
                LobbySettings = state.LobbySettings with { LobbyOwnershipClaim = context.OwnerClaim.Timestamp.ToUnixTimeMilliseconds() }
            };
            
            await SetTypoStateSettings(context, state.LobbySettings);
            logger.LogDebug("Updated ownership claim of lobby");
            return new LobbyOwnerClaimResult(true, state);
        }
    }
    
    public async Task UpdateTypoLobbySettings(LobbyContext context, SkribblLobbyTypoSettingsUpdateDto settings)
    {
        logger.LogTrace("UpdateTypoLobbySettings(context={context}, settings={settings})", context, settings);
        
        var state = await GetTypoStateSettings(context);
        if (!state.PlayerIsOwner) throw new UnauthorizedAccessException("Player is not owner of lobby");
        
        var newSettings = state.LobbySettings with
        {
            Description = settings.Description, 
            WhitelistAllowedServers = settings.WhitelistAllowedServers, 
            AllowedServers = settings.AllowedServers
        };
        
        using (await KeyedSemaphore.LockAsync(context.OwnerClaim.LobbyId))
        {
            await SetTypoStateSettings(context, newSettings);
        }
    }
    
    public void SaveSkribblLobbyState(LobbyContext context, SkribblLobbyStateDto state)
    {
        logger.LogTrace("SaveSkribblLobbyState(context={context}, state={state})", context, state);
        
        var currentState = lobbyStore.GetSkribblState(context.OwnerClaim.LobbyId);
        
        if(currentState is null || currentState.Record.GetHashCode() != state.GetHashCode())
        {
            lobbyStore.SetSkribblState(context.OwnerClaim.LobbyId, state);
            logger.LogDebug("Skribbl lobby state updated");
        }
        else
        {
            lobbyStore.TouchStateTimestamp(context.OwnerClaim.LobbyId);
            logger.LogDebug("Skribbl lobby state unchanged");
        }
    }
    
    public SkribblLobbyStateDto GetSkribblLobbyState(LobbyContext context)
    {
        logger.LogTrace("GetSkribblLobbyState(context={context})", context);
        
        var state = lobbyStore.GetSkribblState(context.OwnerClaim.LobbyId);
        if(state is null)
        {
            throw new EntityNotFoundException("No state found for lobby");
        }
        if(state.Timestamp.AddSeconds(60) < DateTimeOffset.UtcNow)
        {
            throw new EntityExpiredException("Existing state has expired");
        }
        
        return state.Record;
    }
    
    public void RemoveSkribblLobbyState(LobbyContext context)
    {
        logger.LogTrace("RemoveSkribblLobbyState(context={context})", context);
        
        lobbyStore.RemoveSkribblState(context.OwnerClaim.LobbyId);
        logger.LogDebug("Skribbl lobby state removed");
    }
}