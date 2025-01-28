using tobeh.Avallone.Server.Classes.Dto;

namespace tobeh.Avallone.Server.Classes;

public record LobbyContext(LobbyOwnerClaim OwnerClaim, int PlayerId, int PlayerLogin, List<long> ServerConnections, long? LastClaimedDropId = null);