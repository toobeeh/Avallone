using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Valmar;
using TypedSignalR.Client;

namespace tobeh.Avallone.Server.Hubs.Interfaces;

[Hub]
public interface ILobbyHub
{
    Task<TypoLobbyStateDto> LobbyDiscovered(LobbyDiscoveredDto lobbyDiscovery);
    Task ClaimLobbyOwnership();
    Task UpdateSkribblLobbyState(SkribblLobbyStateDto state);
    Task UpdateTypoLobbySettings(SkribblLobbyTypoSettingsUpdateDto typoSettings);
}

[Receiver]
public interface ILobbyReceiver
{
    Task TypoLobbySettingsUpdated(TypoLobbySettingsDto settings);
    Task LobbyOwnershipResigned();
}