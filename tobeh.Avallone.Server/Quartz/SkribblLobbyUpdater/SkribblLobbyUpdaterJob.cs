using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Hubs;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar;
using tobeh.Valmar.Client.Util;

namespace tobeh.Avallone.Server.Quartz.SkribblLobbyUpdater;

public class SkribblLobbyUpdaterJob(
    ILogger<SkribblLobbyUpdaterJob> logger, 
    Lobbies.LobbiesClient lobbiesClient,
    LobbyService lobbyService,
    LobbyContextStore lobbyContextStore
    ) : IJob
{
    
    /// <summary>
    /// Get all online players, filter saved lobbies for them
    /// Write valid lobbies & status to persistance via valmar
    /// Broadcast guild lobbies to subscribing clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogTrace("Execute({context})", context);

        var onlinePlayers = lobbyContextStore.RetrieveExistingLobbyContexts();

        var lobbies = onlinePlayers
            .GroupBy(player => player.OwnerClaim.LobbyId)
            .Select(group =>
            {
                try
                {
                    var lobby = new
                    {
                        Players = group.ToList(),
                        SkribblLobby = lobbyService.GetSkribblLobbyState(group.First())
                    };
                    return lobby;
                }
                catch (Exception e)
                {
                    if (e is EntityExpiredException) lobbyService.RemoveSkribblLobbyState(group.First());
                    else if (e is not EntityNotFoundException) throw;
                    return null;
                }
            })
            .Where(lobby => lobby is not null)
            .Select(lobby => lobby!)
            .ToList();
        
        // write status and lobbies to valmar, write guild lobbies to clients
    }
}