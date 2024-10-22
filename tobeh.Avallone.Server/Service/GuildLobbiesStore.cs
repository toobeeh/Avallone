using System.Collections.Concurrent;
using tobeh.Avallone.Server.Dto;

namespace tobeh.Avallone.Server.Service;

public class GuildLobbiesStore(
    ILogger<GuildLobbiesStore> logger
)
{
    private readonly ConcurrentDictionary<string, List<GuildLobbyDto>> _guildLobbies = new();

    public bool SetLobbiesForGuild(string guildId, List<GuildLobbyDto> lobbies)
    {
        logger.LogTrace("SetLobbiesForGuild(guildId={guildId}, lobbies={lobbies})", guildId, lobbies);

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
    
    public void Reset()
    {
        logger.LogTrace("Reset()");
        _guildLobbies.Clear();
    }
}