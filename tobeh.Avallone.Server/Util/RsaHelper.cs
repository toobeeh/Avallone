using System.Security.Cryptography;
using System.Text;
using tobeh.Avallone.Server.Classes;

namespace tobeh.Avallone.Server.Util;

public static class RsaHelper
{
    
    public static LobbyOwnerClaim CreateOwnerClaim(RSA rsa, string lobbyId, DateTimeOffset timestamp)
    {
        var bytes = rsa.Encrypt(Encoding.UTF8.GetBytes($"{lobbyId}:{timestamp.ToUnixTimeMilliseconds()}"), RSAEncryptionPadding.Pkcs1);
        var token = Convert.ToBase64String(bytes);
        return new LobbyOwnerClaim(timestamp, lobbyId, token);
    }

    public static LobbyOwnerClaim DecryptOwnerClaim(RSA rsa, string token)
    {
        var content = rsa.Decrypt(Convert.FromBase64String(token), RSAEncryptionPadding.Pkcs1);
        var stringContent = Encoding.UTF8.GetString(content);
        var id = stringContent.Split(":")[0];
        var ticks = stringContent[(id.Length + 1)..];
        return new LobbyOwnerClaim(DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(ticks)), id, token);
    }
    
    public static string CreateDropToken(RSA rsa, AnnouncedDropDetails dropDetails)
    {
        var bytes = rsa.Encrypt(Encoding.UTF8.GetBytes($"{dropDetails.DropId}:{dropDetails.AnnouncementTimestamp.ToUnixTimeMilliseconds()}"), RSAEncryptionPadding.Pkcs1);
        var token = Convert.ToBase64String(bytes);
        return token;
    }

    public static AnnouncedDropDetails ParseDropToken(RSA rsa, string token)
    {
        var content = rsa.Decrypt(Convert.FromBase64String(token), RSAEncryptionPadding.Pkcs1);
        var stringContent = Encoding.UTF8.GetString(content);
        var id = stringContent.Split(":")[0];
        var timestamp = stringContent[(id.Length + 1)..];
        return new AnnouncedDropDetails(
            Convert.ToInt64(id),
            DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timestamp))
        );
    }
}