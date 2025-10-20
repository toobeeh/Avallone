using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Service;

public class MemberContextCache(ILogger<MemberContextCache> logger)
{
    // sub (id) -> member
    private readonly ConcurrentDictionary<string, TimestampedRecord<MemberReply>> _memberContextCache = new ();
    
    public void CacheMemberForId(string memberId, MemberReply member)
    {
        logger.LogTrace("CacheMember(memberId={memberId}, member.Login={member.Login})", memberId, member.Login);
        
        var timestampedRecord = new TimestampedRecord<MemberReply>(DateTimeOffset.UtcNow, member);
        _memberContextCache[memberId] = timestampedRecord;
    }
    
    public MemberReply? GetCachedMemberForId(string memberId)
    {
        logger.LogTrace("GetCachedMember(memberId={memberId})", memberId);

        // not yet cached
        if (!_memberContextCache.TryGetValue(memberId, out var timestampedRecord))
        {
            return null;
        }

        // cached and not expired
        if (timestampedRecord.Timestamp.AddSeconds(10) >= DateTimeOffset.UtcNow)
        {
            return timestampedRecord.Record;
        }
        
        // cached but expired
        logger.LogDebug("Cached member for memberId={memberId} is expired", memberId);
        _memberContextCache.TryRemove(memberId, out _);
        return null;
    }
    
    public void InvalidateCacheForId(string memberId)
    {
        logger.LogTrace("InvalidateCache(connectionId={memberId})", memberId);
        
        _memberContextCache.TryRemove(memberId, out _);
    }
}