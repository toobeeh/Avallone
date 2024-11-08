using tobeh.Avallone.Server.Classes.Dto;
using tobeh.Valmar;
using TypedSignalR.Client;

namespace tobeh.Avallone.Server.Hubs.Interfaces;

[Hub]
public interface IOnlineItemsHub;

[Receiver]
public interface IOnlineItemsReceiver
{
    public Task OnlineItemsUpdated(OnlineItemsUpdatedDto itemUpdates);
}