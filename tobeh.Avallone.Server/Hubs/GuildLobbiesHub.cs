using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using tobeh.Avallone.Server.Authentication;
using tobeh.Avallone.Server.Dto;
using tobeh.Avallone.Server.Hubs.Interfaces;
using tobeh.Avallone.Server.Service;
using tobeh.Valmar;

namespace tobeh.Avallone.Server.Hubs;

public class GuildLobbiesHub(
    ILogger<GuildLobbiesHub> logger, 
    Guilds.GuildsClient guildsClient,
    GuildLobbiesStore guildLobbiesStore
    ) : Hub<IGuildLobbiesReceiver>, IGuildLobbiesHub
{
    [Authorize]
    public async Task<GuildLobbiesUpdatedDto> SubscribeGuildLobbies(string guildId)
    {
        logger.LogTrace("SubscribeGuildLobbies(guildId={guildId})", guildId);
        
        var guild = await guildsClient.GetGuildByIdAsync(new GetGuildByIdMessage { DiscordId = Convert.ToInt64(guildId) });
        if (guild is null)
        {
            logger.LogWarning("Requested guild not found");
            throw new NullReferenceException("Guild does not exist");
        }

        var authorized = Context.User?.Claims
            .Where(claim => claim.Type == TypoTokenDefaults.GuildClaimName)
            .Any(claim => claim.Value == guild.Invite.ToString()) ?? false;
        
        if(!authorized)
        {
            logger.LogWarning("Unauthorized access to subscribe guild lobbies");
            throw new UnauthorizedAccessException("Unauthorized access to subscribe guild lobbies");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, guild.GuildId.ToString());
        return new GuildLobbiesUpdatedDto(guildId, guildLobbiesStore.GetLobbiesOfGuild(guild.GuildId.ToString()));
    }
}