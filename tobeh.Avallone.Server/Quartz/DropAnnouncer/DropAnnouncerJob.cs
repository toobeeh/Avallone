using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Quartz.DropAnnouncer;

public class DropAnnouncerJob(
    ILogger<DropAnnouncerJob> logger, 
    Drops.DropsClient dropsClient,
    IHubContext<LobbyHub, ILobbyReceiver> lobbyHubContext
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogTrace("Execute({context})", context);

        try
        {
            await AnnounceDrop();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to announce drop");
        }
        
        /* schedule next drop check in 1s - min delay is 2s, so shannon is satisfied ;) */
        var newTrigger = TriggerBuilder.Create()
            .StartAt(DateTimeOffset.Now.AddSeconds(1))
            .Build();

        logger.LogDebug("Next check in 1s");
        await context.Scheduler.RescheduleJob(context.Trigger.Key, newTrigger);
    }

    private async Task AnnounceDrop()
    {
        logger.LogTrace("AnnounceDrop()");
        
        var drop = await dropsClient.GetScheduledDropAsync(new Empty());
        var dropTime = drop.Timestamp.ToDateTimeOffset();
        
        /* if drop is announced, wait until drop and announce */
        if(dropTime > DateTimeOffset.UtcNow)
        {
            logger.LogInformation("Drop {dropId} is scheduled for {dropTime}", drop.Id, dropTime);
            
            var position = Convert.ToInt32(drop.Id % 100);
            await Task.Delay((int)(dropTime - DateTimeOffset.Now).TotalMilliseconds);
            await lobbyHubContext.Clients.All.DropAnnounced(new DropAnnouncementDto(drop.Id, drop.EventDropId, position));
            logger.LogInformation("Drop {dropId} announced", drop.Id);
        }
    }
}