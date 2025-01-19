using Tapper;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record AwardGiftedDto(int ReceiverLobbyPlayerId, int AwarderLobbyPlayerId, int AwardInventoryId, int AwardId);