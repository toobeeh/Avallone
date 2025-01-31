using tobeh.Avallone.Server.Classes;
using tobeh.Avallone.Server.Service;

namespace tobeh.Avallone.Server.Util;

public static class CryptoHelper
{
    
    public static LobbyOwnerClaim CreateOwnerClaim(CryptoService crypto, string lobbyId, DateTimeOffset timestamp)
    {
        var token = crypto.EncryptIvPrepended($"{lobbyId}:{timestamp.ToUnixTimeMilliseconds()}");
        return new LobbyOwnerClaim(timestamp, lobbyId, token);
    }

    public static LobbyOwnerClaim DecryptOwnerClaim(CryptoService crypto, string token)
    {
        var content = crypto.DecryptIvPrepended(token);
        var id = content.Split(":")[0];
        var ticks = content[(id.Length + 1)..];
        return new LobbyOwnerClaim(DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(ticks)), id, token);
    }
    
    public static string CreateDropToken(CryptoService crypto, AnnouncedDropDetails dropDetails)
    {
        var token = crypto.EncryptIvPrepended($"{dropDetails.DropId}:{dropDetails.AnnouncementTimestamp.ToUnixTimeMilliseconds()}");
        return token;
    }

    public static AnnouncedDropDetails ParseDropToken(CryptoService crypto, string token)
    {
        var content = crypto.DecryptIvPrepended(token);
        var id = content.Split(":")[0];
        var timestamp = content[(id.Length + 1)..];
        return new AnnouncedDropDetails(
            Convert.ToInt64(id),
            DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timestamp))
        );
    }
}