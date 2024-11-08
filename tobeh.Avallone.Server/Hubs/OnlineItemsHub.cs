using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using tobeh.Avallone.Server.Authentication;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Hubs;

public class OnlineItemsHub(
    ILogger<OnlineItemsHub> logger, 
    OnlineItemsStore onlineItemsStore
    ) : Hub<IOnlineItemsReceiver>, IOnlineItemsHub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogTrace("OnConnectedAsync()");
        
        /* send current items initially */
        var items = await onlineItemsStore.GetOnlineItems();
        await Clients.Caller.OnlineItemsUpdated(new OnlineItemsUpdatedDto(items));
        logger.LogDebug("Sent {items} initial online items to {connectionId}", items.Count, Context.ConnectionId);
    }
}