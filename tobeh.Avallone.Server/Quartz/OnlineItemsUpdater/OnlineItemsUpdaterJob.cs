using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar;
using tobeh.Valmar.Client.Util;

namespace tobeh.Avallone.Server.Quartz.OnlineItemsUpdater;

public class OnlineItemsUpdaterJob(
    ILogger<OnlineItemsUpdaterJob> logger, 
    Admin.AdminClient adminClient,
    OnlineItemsStore onlineItemsStore,
    IHubContext<OnlineItemsHub, IOnlineItemsReceiver> onlineItemsHubContext
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogTrace("Execute({context})", context);

        /* get all onlineitems */
        var onlineItems = await adminClient.GetOnlineItems(new Empty()).ToListAsync();
        var onlineItemsDto = onlineItems
            .Select(item => new OnlineItemDto((OnlineItemTypeDto) item.ItemType, item.Slot,item.ItemId, item.LobbyKey, item.LobbyPlayerId))
            .ToList();
        
        /* update store and broadcast if changes happened */
        var changes = await onlineItemsStore.SetOnlineItems(onlineItemsDto);
        if (changes)
        {
            await onlineItemsHubContext.Clients.All.OnlineItemsUpdated(new OnlineItemsUpdatedDto(onlineItemsDto));
            logger.LogDebug("Updated {items} online items", onlineItemsDto.Count);
        }
        else
        {
            logger.LogDebug("No changes in online items");
        }
    }
}