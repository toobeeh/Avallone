using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record TypoLobbySettingsDto(string Description, bool WhitelistAllowedServers, List<string> AllowedServers, long? LobbyOwnershipClaim);

[TranspilationSource]
public record TypoLobbyStateDto(bool PlayerIsOwner, string OwnershipClaimToken, string LobbyId, long OwnershipClaim, TypoLobbySettingsDto LobbySettings);

[TranspilationSource]
public record SkribblLobbyPlayerDto(string Name, int PlayerId, int Score, bool IsDrawing, bool HasGuessed);

[TranspilationSource]
public record SkribblLobbySettingsDto(string Language, int MaxPlayers);

[TranspilationSource]
public sealed record SkribblLobbyStateDto(
    string Link,
    bool Custom,
    List<SkribblLobbyPlayerDto> Players,
    SkribblLobbySettingsDto Settings)
{
    public override int GetHashCode()
    {
        // Combine the hash codes of each property, including Players
        var hash = HashCode.Combine(Link, Custom, Settings);
        return Players.Aggregate(hash, HashCode.Combine);
    }
}

[TranspilationSource]
public record LobbyStateDto(SkribblLobbyStateDto SkribblState, TypoLobbyStateDto TypoState);