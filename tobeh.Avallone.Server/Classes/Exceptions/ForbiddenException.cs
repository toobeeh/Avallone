using Microsoft.AspNetCore.SignalR;

namespace tobeh.Avallone.Server.Classes.Exceptions;

public class ForbiddenException(string message) : HubException(message);