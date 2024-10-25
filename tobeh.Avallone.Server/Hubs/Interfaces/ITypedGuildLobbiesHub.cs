using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Valmar;
using TypedSignalR.Client;

namespace tobeh.Avallone.Server.Hubs.Interfaces;

[Hub]
public interface IGuildLobbiesHub
{
    public Task<GuildLobbiesUpdatedDto> SubscribeGuildLobbies(string guildId);
}

[Receiver]
public interface IGuildLobbiesReceiver
{
    public Task GuildLobbiesUpdated(GuildLobbiesUpdatedDto lobbyUpdates);
}