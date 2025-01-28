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
    Task<DropClaimResultDto> ClaimDrop(DropClaimDto dropClaim);
    Task GiftAward(AwardGiftDto awardGift);
}

[Receiver]
public interface ILobbyReceiver
{
    Task TypoLobbySettingsUpdated(TypoLobbySettingsDto settings);
    Task LobbyOwnershipResigned();
    Task DropAnnounced(DropAnnouncementDto drop);
    Task AwardGifted(AwardGiftedDto award);
    Task DropClaimed(DropClaimResultDto claimResult);
    Task DropCleared(DropClearDto drop);
}