using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record SkribblLobbyTypoSettingsUpdateDto(bool WhitelistAllowedServers, List<string> AllowedServers, string Description);