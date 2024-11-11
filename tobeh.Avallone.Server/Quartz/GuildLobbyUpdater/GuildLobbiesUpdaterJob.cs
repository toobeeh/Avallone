using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar;
using tobeh.Valmar.Client.Util;

namespace tobeh.Avallone.Server.Quartz.GuildLobbyUpdater;

public class GuildLobbiesUpdaterJob(
    ILogger<GuildLobbiesUpdaterJob> logger, 
    Lobbies.LobbiesClient lobbiesClient,
    GuildLobbiesStore guildLobbiesStore,
    IHubContext<GuildLobbiesHub, IGuildLobbiesReceiver> guildLobbiesHubContext
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogTrace("Execute({context})", context);

        /* get all guild lobbies links */
        var lobbies = await lobbiesClient.GetLobbyLinks(new Empty()).ToListAsync();
        var guildLobbies =
            lobbies.GroupBy(lobby => lobby.GuildId)
                .Select(group => new
                {
                    Id = group.Key.ToString(),
                    Lobbies = group.Select(lobby => new GuildLobbyDto(lobby.Username, 1, "", lobby.Link, false))
                        .ToList() // TODO refactor valmar to get actual details for lobby
                });

        /* save updates to store and push to clients */
        guildLobbiesStore.BeginReset();
        var updates = guildLobbies.Select(async guild =>
        {
            var changes = guildLobbiesStore.SetLobbiesForGuild(guild.Id, guild.Lobbies);
            if (changes)
            {
                await guildLobbiesHubContext.Clients.Group(guild.Id)
                    .GuildLobbiesUpdated(new GuildLobbiesUpdatedDto(guild.Id, guild.Lobbies));
        
                logger.LogDebug("Updated guild lobbies of {id}", guild.Id);
            }
        }).ToList();
        await Task.WhenAll(updates);
        guildLobbiesStore.ResetUnchanged();
    }
}