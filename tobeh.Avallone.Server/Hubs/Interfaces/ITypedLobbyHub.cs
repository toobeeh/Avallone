using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Valmar;
using TypedSignalR.Client;

namespace tobeh.Avallone.Server.Hubs.Interfaces;

[Hub]
public interface ILobbyHub
{
    Task<TypoLobbyStateDto> LobbyDiscovered(LobbyDiscoveredDto lobbyDiscovery);
    Task ClaimLobbyOwnership();
}

[Receiver]
public interface ILobbyReceiver
{
    Task TypoLobbyStateUpdated(TypoLobbyStateDto state);
    Task LobbyOwnershipResigned();
}