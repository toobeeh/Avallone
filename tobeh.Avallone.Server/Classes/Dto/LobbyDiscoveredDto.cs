using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record LobbyDiscoveredDto(SkribblLobbyStateDto Lobby, string? OwnerClaimToken, int PlayerId);