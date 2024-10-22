using Tapper;

namespace tobeh.Avallone.Server.Dto;

[TranspilationSource]
public record GuildLobbiesUpdatedDto(string GuildId, List<GuildLobbyDto> Lobbies);