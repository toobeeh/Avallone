using Microsoft.AspNetCore.SignalR;

namespace tobeh.Avallone.Server.Classes.Exceptions;

public class EntityNotFoundException(string message) : HubException(message);