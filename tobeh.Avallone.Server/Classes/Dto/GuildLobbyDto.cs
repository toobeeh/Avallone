using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record GuildLobbyDto(string UserName, int CurrentPlayers, string Language, string LobbyId, bool Private);