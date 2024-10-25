using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record GuildLobbiesUpdatedDto(string GuildId, List<GuildLobbyDto> Lobbies);