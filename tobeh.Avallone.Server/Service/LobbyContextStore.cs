using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Avallone.Server.Classes.Exceptions;
using tobeh.Avallone.Server.Util;

namespace tobeh.Avallone.Server.Service;

public class LobbyContextStore(ILogger<LobbyContextStore> logger)
{
    private readonly ConcurrentDictionary<string, LobbyContext> _connections = new();
    private readonly ConcurrentDictionary<string, LobbyContext> _detachedPending = new();
    private readonly SemaphoreSlim _detachedPendingLock = new(1);

    public LobbyContext AttachContextToClient(string id, string lobbyId, int playerId, int playerLogin, List<long> serverConnections, string? ownerClaim = null)
    {
        logger.LogTrace("AttachContextToClient(id={id}, lobbyId={lobbyId}, ownerClaim={ownerClaim})", id, lobbyId, ownerClaim);
        
        var claim = ownerClaim != null ? 
            RsaHelper.DecryptOwnerClaim(ownerClaim) : 
            RsaHelper.CreateOwnerClaim(lobbyId, DateTimeOffset.Now);
        
        var context = new LobbyContext(claim, playerId, playerLogin, serverConnections);
        _connections.AddOrUpdate(id, context, (key, oldValue) => context);

        return context;
    }

    public async Task DetachContextFromClient(string id)
    {
        logger.LogTrace("DetachContextFromClient(id={id})", id);
        
        _connections.Remove(id, out var value);
        if (value is not null)
        {
            await _detachedPendingLock.WaitAsync();
            _detachedPending.AddOrUpdate(id, value, (key, oldValue) => value);
            _detachedPendingLock.Release();
        }
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
    
    public async Task<Dictionary<string, LobbyContext>> FlushDetachedPending()
    {
        logger.LogTrace("FlushDetachedPending()");

        await _detachedPendingLock.WaitAsync();
        var entries = _detachedPending.ToDictionary();
        _detachedPending.Clear();
        _detachedPendingLock.Release();

        return entries;
    }

    public void MarkDropAsClaimed(string clientId, long dropId)
    {
        logger.LogTrace("MarkDropAsClaimed(clientId={clientId}, dropId={dropId})", clientId, dropId);
        
        if (!_connections.TryGetValue(clientId, out var context))
        {
            throw new EntityNotFoundException("No lobby context attached for connection id");
        }
        
        if (context.LastClaimedDropId == dropId)
        {
            throw new ForbiddenException("Drop already claimed");
        }
        
        var updated = _connections.TryUpdate(clientId, context with { LastClaimedDropId = dropId }, context);
        
        if (!updated)
        {
            throw new InvalidOperationException("Failed to update lobby context");
        }
    }
    
}