namespace tobeh.Avallone.Server.Classes;

public record LobbyOwnerClaim(DateTimeOffset Timestamp, string LobbyId, string Token);