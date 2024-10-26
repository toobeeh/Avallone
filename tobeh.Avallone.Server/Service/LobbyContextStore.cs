using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Util;

namespace tobeh.Avallone.Server.Service;

public class LobbyContextStore(ILogger<LobbyContextStore> logger)
{
    private readonly ConcurrentDictionary<string, LobbyContext> _connections = new();

    public async Task<LobbyContext> AttachContextToClient(string id, string lobbyId, int playerId, int playerLogin, List<long> serverConnections, string? ownerClaim = null)
    {
        logger.LogTrace("AttachContextToClient(id={id}, lobbyId={lobbyId}, ownerClaim={ownerClaim})", id, lobbyId, ownerClaim);
        
        var claim = ownerClaim != null ? 
            RsaHelper.DecryptOwnerClaim(ownerClaim) : 
            RsaHelper.CreateOwnerClaim(lobbyId, DateTimeOffset.Now);
        
        var context = new LobbyContext(claim, playerId, playerLogin, serverConnections);
        _connections.AddOrUpdate(id, context, (key, oldValue) => context);

        return context;
    }

    public void DetachContextFromClient(string id)
    {
        logger.LogTrace("DetachContextFromClient(id={id})", id);
        
        _connections.Remove(id, out var value);
    }

    public LobbyContext RetrieveContextFromClient(string id)
    {
        logger.LogTrace("RetrieveContextFromClient(id={id})", id);
        
        if (!_connections.TryGetValue(id, out var context))
        {
            throw new EntityNotFoundException("No lobby context attached for connection id");
        }

        return context;
    }

    public List<LobbyContext> RetrieveExistingLobbyContexts()
    {
        logger.LogTrace("RetrieveExistingLobbyContexts()");
        
        return _connections.Values.ToList();
    }
    
}