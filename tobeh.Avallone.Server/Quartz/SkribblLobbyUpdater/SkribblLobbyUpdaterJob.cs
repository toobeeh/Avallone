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
        
        // crate player update messages
        var playerMessages = lobbies.Select(lobby => new SkribblLobbyTypoMembersMessage
        {
            LobbyId = lobby.SkribblLobby.Link,
            Members = { lobby.Players.Select(member => new SkribblLobbyTypoMemberMessage()
            {
                Login = member.PlayerLogin,
                LobbyPlayerId = member.PlayerId,
                OwnershipClaim = member.OwnerClaim.Timestamp.ToUnixTimeMilliseconds()
            }) }
        });
        
        // if there are detached contexts which come from lobbies that are not refreshed, update with zero players to clear
        var detachedPending = await lobbyContextStore.FlushDetachedPending();
        var detachedPlayerMessages = detachedPending
            .Select(item => item.Value)
            .GroupBy(item => item.OwnerClaim.LobbyId)
            .Select(lobby => new SkribblLobbyTypoMembersMessage
        {
            LobbyId = lobby.Key,
            Members =
            {
                lobby.Select(player => new SkribblLobbyTypoMemberMessage
                {
                    Login = player.PlayerLogin,
                    LobbyPlayerId = player.PlayerId,
                    OwnershipClaim = player.OwnerClaim.Timestamp.ToUnixTimeMilliseconds()
                })
            }
        }).ToList();

        // create lobby update messages
        var lobbyMessages = lobbies.Select(lobby => new SkribblLobbyStateMessage
        {
            LobbyId = lobby.SkribblLobby.Link,
            DrawerId = lobby.SkribblLobby.Players.FirstOrDefault(player => player.IsDrawing)?.PlayerId,
            OwnerId = lobby.SkribblLobby.OwnerId,
            Round = lobby.SkribblLobby.Round,
            Settings = new SkribblLobbySkribblSettingsMessage
            {
                DrawTime = lobby.SkribblLobby.Settings.DrawTime,
                Language = lobby.SkribblLobby.Settings.Language,
                Players = lobby.SkribblLobby.Settings.Players,
                Rounds = lobby.SkribblLobby.Settings.Rounds
            },
            Players =
            {
                lobby.SkribblLobby.Players.Select(player => new SkribblLobbySkribblPlayerMessage
                {
                    Name = player.Name,
                    Guessed = player.HasGuessed,
                    PlayerId = player.PlayerId,
                    Score = player.Score
                })
            }
        });
        
        // send messages to valmar
        List<Task> tasks = [
            .. lobbyMessages.Select(async message => await lobbiesClient.SetSkribblLobbyStateAsync(message)).ToArray(),
            .. playerMessages.Select(async message => await lobbiesClient.SetMemberStatusesInLobbyAsync(message)).ToArray(),
            .. detachedPlayerMessages.Select(async message => await lobbiesClient.RemoveMemberStatusesInLobbyAsync(message)).ToArray()
        ];

        await Task.WhenAll(tasks);
        
        logger.LogDebug("Updated {lobbies} lobbies and their players, detached {detached}", lobbies.Count, detachedPlayerMessages.Count);
    }
}