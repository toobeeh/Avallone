using Tapper;

namespace tobeh.Avallone.Server.Dto;

[TranspilationSource]
public record GuildLobbyDto(string UserName, int CurrentPlayers, string Language, string Invite, bool Private);