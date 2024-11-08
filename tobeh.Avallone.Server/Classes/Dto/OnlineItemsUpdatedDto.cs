using Tapper;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Classes.Dto;

[TranspilationSource]
public record OnlineItemsUpdatedDto(List<OnlineItemDto> Items);

[TranspilationSource]
public record OnlineItemDto(OnlineItemTypeDto Type, int Slot, int ItemId, string LobbyKey, int LobbyPlayerId);

[TranspilationSource]
public enum OnlineItemTypeDto : int
{
    Award = OnlineItemType.Award,
    Sprite = OnlineItemType.Sprite,
    SpriteShift = OnlineItemType.ColorShift,
    Scene = OnlineItemType.Scene,
    SceneTheme = OnlineItemType.SceneTheme,
    Rewardee = OnlineItemType.Rewardee
}