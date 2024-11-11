using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes.Dto;

namespace tobeh.Avallone.Server.Service;

public class GuildLobbiesStore(
    ILogger<GuildLobbiesStore> logger
)
{
    private readonly ConcurrentDictionary<string, List<GuildLobbyDto>> _guildLobbies = new();
    private readonly ConcurrentDictionary<string, bool> _resetBlacklist = new();

    public bool SetLobbiesForGuild(string guildId, List<GuildLobbyDto> lobbies)
    {
        logger.LogTrace("SetLobbiesForGuild(guildId={guildId}, lobbies={lobbies})", guildId, lobbies);
        
        _resetBlacklist.AddOrUpdate(guildId, true, (key, oldValue) => true);

        var containsChanges = false;
        _guildLobbies.AddOrUpdate(guildId, lobbies, (key, oldValue) =>
        {
            containsChanges = (oldValue.Count != lobbies.Count) || oldValue.Except(lobbies).Any();
            return lobbies;
        });

        return containsChanges;
    }
    
    public List<GuildLobbyDto> GetLobbiesOfGuild(string guildId)
    {
        logger.LogTrace("GetLobbiesOfGuild(guildId={guildId})", guildId);
        
        _guildLobbies.TryGetValue(guildId, out var lobbies);
        return lobbies ?? [];
    }
    
    public void BeginReset()
    {
        logger.LogTrace("BeginReset()");
        _resetBlacklist.Clear();
    }

    public void ResetUnchanged()
    {
        logger.LogTrace("ResetUnchanged()");
        
        var unchanged = _guildLobbies.Keys.Except(_resetBlacklist.Keys);
        foreach (var guildId in unchanged)
        {
            _guildLobbies.TryRemove(guildId, out _);
        }
    }
}