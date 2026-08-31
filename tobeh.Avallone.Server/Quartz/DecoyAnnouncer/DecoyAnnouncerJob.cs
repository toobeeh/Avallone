using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Avallone.Server.Util;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Quartz.DecoyAnnouncer;

public class DecoyAnnouncerJob(
    ILogger<DecoyAnnouncerJob> logger, 
    Drops.DropsClient dropsClient,
    IHubContext<LobbyHub, ILobbyReceiver> lobbyHubContext,
    CryptoService cryptoService
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
    }

    private async Task AnnounceDrop()
    {
        logger.LogTrace("AnnounceDrop()");
        
        var position = -100; // out of bounds position for decoy drops
        var dropId = 1; // invalid drop id for decoy drops

        var dispatchTimestamp = DateTimeOffset.Now;
        var dropToken = CryptoHelper.CreateDropToken(cryptoService, new AnnouncedDropDetails(dropId, dispatchTimestamp));
        await lobbyHubContext.Clients.All.DropAnnounced(new DropAnnouncementDto(dropToken, dropId, -1, position));
            
        logger.LogInformation("Decoy Drop announced");
    }
}