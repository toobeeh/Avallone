using tobeh.Avallone.Server.Classes.Dto;

namespace tobeh.Avallone.Server.Classes;

public record LobbyOwnerClaimResult(bool ClaimSuccessful, TypoLobbyStateDto State);