using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Util;

namespace tobeh.Avallone.Server.Service;

public class LobbyStore(ILogger<LobbyStore> logger)
{
    private readonly ConcurrentDictionary<string, TimestampedRecord<SkribblLobbyStateDto>> _skribblStates = new();
    
    public void SetSkribblState(string lobbyId, SkribblLobbyStateDto state)
    {
        logger.LogTrace("SetSkribblState(lobbyId={lobbyId}, state={state})", lobbyId, state);
        
        _skribblStates.AddOrUpdate(lobbyId, new TimestampedRecord<SkribblLobbyStateDto>(DateTimeOffset.UtcNow,  state), (key, oldValue) => new TimestampedRecord<SkribblLobbyStateDto>(DateTimeOffset.UtcNow, state));
    }
    
    public void TouchStateTimestamp(string lobbyId)
    {
        logger.LogTrace("TouchStateTimestamp(lobbyId={lobbyId})", lobbyId);
        
        _skribblStates.TryGetValue(lobbyId, out var record);
        if (record != null)
        {
            _skribblStates.TryUpdate(lobbyId, new TimestampedRecord<SkribblLobbyStateDto>(DateTimeOffset.UtcNow, record.Record), record);
        }
    }
    
    public TimestampedRecord<SkribblLobbyStateDto>? GetSkribblState(string lobbyId)
    {
        logger.LogTrace("GetSkribblState(lobbyId={lobbyId})", lobbyId);
        
        _skribblStates.TryGetValue(lobbyId, out var record);
        return record;
    }

    public void RemoveSkribblState(string lobbyId)
    {
        logger.LogTrace("RemoveSkribblState(lobbyId={lobbyId})", lobbyId);

        _skribblStates.TryRemove(lobbyId, out var _);
    }

}