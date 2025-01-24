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
    Members.MembersClient membersClient,
    GuildLobbiesStore guildLobbiesStore,
    IHubContext<GuildLobbiesHub, IGuildLobbiesReceiver> guildLobbiesHubContext
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogTrace("Execute({context})", context);
        
        /* get all lobbies with online members */
        var memberLobbies = await lobbiesClient
            .GetOnlineLobbyPlayers(new GetOnlinePlayersRequest())
            .ToListAsync();
        
        /* get all members that are online */
        var memberLogins = memberLobbies.SelectMany(lobby => lobby.Members.Select(member => member.Login));
        var members =await  membersClient
            .GetMembersByLogin(new GetMembersByLoginMessage { Logins = { memberLogins } })
            .ToDictionaryAsync(member => member.Login);
        
        /* get all skribbl lobby details */
        var lobbyIds = memberLobbies.Select(lobby => lobby.LobbyId);
        var lobbies = await lobbiesClient
            .GetLobbiesById(new GetLobbiesByIdRequest { LobbyIds = { lobbyIds } })
            .ToDictionaryAsync(lobby => lobby.SkribblState.LobbyId);
        
        /* for each lobby member, get the lobby details and their connected guilds and add to guild lobbies */
        var guildLobbies = new Dictionary<long, List<GuildLobbyDto>>();
        foreach (var lobby in memberLobbies) 
        {
            var lobbyDetails = lobbies[lobby.LobbyId];
            
            /* lobby generally restricted */
            if(lobbyDetails.TypoSettings.WhitelistAllowedServers && lobbyDetails.TypoSettings.AllowedServers.Count == 0) continue;
            
            foreach (var lobbyMember in lobby.Members)
            {
                var member = members[lobbyMember.Login];
                var lobbyPlayer = lobbyDetails.SkribblState.Players
                    .FirstOrDefault(p => p.PlayerId == lobbyMember.LobbyPlayerId);
                if (lobbyPlayer is null) continue;

                var guildLobby = new GuildLobbyDto(
                    lobbyPlayer.Name,
                    lobbyDetails.SkribblState.Players.Count,
                    lobbyDetails.SkribblState.Settings.Language,
                    lobbyDetails.SkribblState.LobbyId,
                    lobbyDetails.SkribblState.OwnerId is not null
                );

                /* add only to servers of member where whitelisted */
                var servers = lobbyDetails.TypoSettings.WhitelistAllowedServers
                    ? member.ServerConnections.Intersect(lobbyDetails.TypoSettings.AllowedServers)
                    : member.ServerConnections;
                foreach (var server in servers)
                {

                    if(guildLobbies.TryGetValue(server, out var value)) value.Add(guildLobby);
                    else guildLobbies[server] = [guildLobby];
                }
            }
        }

        /* save updates to store and push to clients */
        guildLobbiesStore.BeginReset();
        var updates = guildLobbies.Select(async guild =>
        {
            var guildId = guild.Key.ToString();
            var changes = guildLobbiesStore.SetLobbiesForGuild(guildId, guild.Value);
            if (changes)
            {
                await guildLobbiesHubContext.Clients.Group(guildId)
                    .GuildLobbiesUpdated(new GuildLobbiesUpdatedDto(guildId, guild.Value));

                logger.LogDebug("Updated guild lobbies of {id}", guildId);
            }
        }).ToList();
        
        await Task.WhenAll(updates);
        
        /* empty other which received no lobbies */
        var emptied =  guildLobbiesStore.ResetUnchanged();
        
        /* notify cleared lobbies */
        await Task.WhenAll(emptied
            .Select(guild => guildLobbiesHubContext.Clients.
                Group(guild)
                .GuildLobbiesUpdated(new GuildLobbiesUpdatedDto(guild, new List<GuildLobbyDto>())
                )
            )
        );
    }
}