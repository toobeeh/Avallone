using System.Collections.Concurrent;
using tobeh.Avallone.Server.Classes.Dto;

namespace tobeh.Avallone.Server.Service;

public class OnlineItemsStore
{
    private readonly List<OnlineItemDto> _onlineItems = [];
    private readonly SemaphoreSlim _lock = new(1);
    
    /// <summary>
    /// Get all current online items
    /// </summary>
    /// <returns></returns>
    public async Task<List<OnlineItemDto>> GetOnlineItems()
    {
        await _lock.WaitAsync();
        var items = _onlineItems.ToList();
        _lock.Release();
        return items;
    }
    
    /// <summary>
    /// Set all online items
    /// </summary>
    /// <param name="onlineItems"></param>
    public async Task<bool> SetOnlineItems(List<OnlineItemDto> onlineItems)
    {
        await _lock.WaitAsync();
        
        // compare the new list with the old list
        var changes = _onlineItems.Count != onlineItems.Count || _onlineItems.Except(onlineItems).Any();

        if (changes)
        {
            _onlineItems.Clear();
            _onlineItems.AddRange(onlineItems);
        }
        
        _lock.Release();
        return changes;
    }
}