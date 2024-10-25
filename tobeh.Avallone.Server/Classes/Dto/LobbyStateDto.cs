using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record TypoLobbySettingsDto(string Description, bool WhitelistAllowedServers, List<string> AllowedServers, long? LobbyOwnershipClaim);

[TranspilationSource]
public record TypoLobbyStateDto(bool PlayerIsOwner, TypoLobbySettingsDto LobbySettings);

[TranspilationSource]
public record SkribblLobbyPlayerDto(string Name, int PlayerId, int Score, bool IsDrawing, bool HasGuessed);

[TranspilationSource]
public record SkribblLobbySettingsDto(string Language, int MaxPlayers);

[TranspilationSource]
public record SkribblLobbyStateDto(string Link, bool Custom, List<SkribblLobbyPlayerDto> Players, SkribblLobbySettingsDto Settings);

[TranspilationSource]
public record LobbyStateDto(SkribblLobbyStateDto SkribblState, TypoLobbyStateDto TypoState);