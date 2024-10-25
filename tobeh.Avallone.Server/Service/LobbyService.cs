using Google.Protobuf.WellKnownTypes;
using KeyedSemaphores;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Service;

public class LobbyService(ILogger<LobbyService> logger, LobbyStore lobbyStore, Lobbies.LobbiesClient lobbiesClient)
{
    public async Task<TypoLobbyStateDto> GetTypoStateSettings(LobbyContext context)
    {
        logger.LogTrace("GetTypoStateSettings(context={context})", context);
        
        var lobby = await lobbiesClient.GetSkribblLobbyTypoSettingsAsync(new SkribblLobbyIdentificationMessage
            { Link = context.OwnerClaim.LobbyId });

        var ownerClaim = lobby.LobbyOwnershipClaim;
        var isOwner = context.OwnerClaim.Timestamp.ToUnixTimeMilliseconds() == ownerClaim;
        
        return new TypoLobbyStateDto(isOwner, 
            new TypoLobbySettingsDto(lobby.Description, lobby.WhitelistAllowedServers, 
                lobby.AllowedServers.Select(s => s.ToString()).ToList(), ownerClaim));
    }
    
    private async Task SaveTypoStateSettings(LobbyContext context, TypoLobbySettingsDto settings)
    {
        logger.LogTrace("SaveTypoStateSettings(settings={settings})", settings);
        
        await lobbiesClient.SaveSkribblLobbyTypoSettingsAsync(new SkribblLobbyTypoSettingsMessage
        {
            LobbyId = context.OwnerClaim.LobbyId,
            Description = settings.Description,
            WhitelistAllowedServers = settings.WhitelistAllowedServers,
            AllowedServers = { settings.AllowedServers.Select(long.Parse) },
            LobbyOwnershipClaim = settings.LobbyOwnershipClaim
        });
    }

    public async Task<bool> RemoveOwnershipFromLobby(LobbyContext context)
    {
        logger.LogTrace("RemoveOwnershipFromLobby(context={context})", context);

        var state = await GetTypoStateSettings(context);
        if (!state.PlayerIsOwner) return false;
        
        using (await KeyedSemaphore.LockAsync(context.OwnerClaim.LobbyId))
        {
            await SaveTypoStateSettings(context, state.LobbySettings with { LobbyOwnershipClaim = null });
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
            await SaveTypoStateSettings(context, state.LobbySettings);
            return new LobbyOwnerClaimResult(true, state);
        }
    }
    
    public void SaveSkribblLobbyState(LobbyContext context, SkribblLobbyStateDto state)
    {
        logger.LogTrace("SaveSkribblLobbyState(context={context}, state={state})", context, state);
        
        var currentState = lobbyStore.GetSkribblState(context.OwnerClaim.LobbyId);
        if(currentState is null || currentState.Record.GetHashCode() != state.GetHashCode())
        {
            lobbyStore.SetSkribblState(context.OwnerClaim.LobbyId, state);
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
        if(state.Timestamp.AddSeconds(5) > DateTimeOffset.UtcNow)
        {
            throw new EntityExpiredException("Existing state has expired");
        }
        
        return state.Record;
    }
    
    public void RemoveSkribblLobbyState(LobbyContext context)
    {
        logger.LogTrace("RemoveSkribblLobbyState(context={context})", context);
        
        lobbyStore.RemoveSkribblState(context.OwnerClaim.LobbyId);
    }
}