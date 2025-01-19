using Microsoft.AspNetCore.Authorization;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Hubs;

public partial class LobbyHub
{
    [Authorize]
    public async Task GiftAward(AwardGiftDto awardGift)
    {
        logger.LogTrace("GiftAward(awardGift={awardGift})", awardGift);
        
        /* get required context details */
        var login = TypoTokenHandlerHelper.ExtractLoginClaim(Context.User?.Claims ?? []);
        var lobbyContext = lobbyContextStore.RetrieveContextFromClient(Context.ConnectionId);
        var lobby = lobbyService.GetSkribblLobbyState(lobbyContext);
        var drawer = lobby.Players.FirstOrDefault(player => player.IsDrawing);

        if (drawer is null)
        {
            throw new EntityNotFoundException("No drawer found in lobby");
        }
        
        /* assign award */
        var award = await inventoryClient.GiveAwardAsync(new GiveAwardMessage
        {
            Login = login, 
            AwardInventoryId = awardGift.AwardInventoryId,
            LobbyId = lobbyContext.OwnerClaim.LobbyId,
            ReceiverLobbyPlayerId = drawer.PlayerId
        });

        /* respond with inv id for claiming and award type id */
        await Clients.Group(lobbyContext.OwnerClaim.LobbyId).AwardGifted(
            new AwardGiftedDto(drawer.PlayerId, lobbyContext.PlayerId, awardGift.AwardInventoryId, award.Id)
        );
    }
}