    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Extensions.Options;
    using CryptoConfig = tobeh.Avallone.Server.Config.CryptoConfig;

    namespace tobeh.Avallone.Server.Service;

    public class CryptoService(IOptions<CryptoConfig> config)
    {
        private byte[]? _key;
        private static byte[] EncryptData(Aes aes, byte[] dataBytes)
        {
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

            cs.Write(dataBytes, 0, dataBytes.Length);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }

        private static string DecryptData(Aes aes, byte[] cipherBytes)
        {
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            
            return reader.ReadToEnd();
        }
        

        public string DecryptIvPrepended(string prependedBase64)
        {
            var prependedBytes = Convert.FromBase64String(prependedBase64);
            var iv = prependedBytes[..16];
            
            using var aes = Aes.Create();
            if (_key == null)
            {
                _key = Convert.FromBase64String(config.Value.Key);
            }
            aes.Key = _key;
            
            aes.IV = iv;
            var cipherBytes = prependedBytes[16..];

            var decrypted = DecryptData(aes, cipherBytes);
            return decrypted;
        }

        public string EncryptIvPrepended(string data)
        {
            using var aes = Aes.Create();
            if (_key == null)
            {
                _key = Convert.FromBase64String(config.Value.Key);
                _key = aes.Key;
            }
            aes.Key = _key;
            
            aes.GenerateIV();
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var cipherBytes = EncryptData(aes, dataBytes);
            var prependedBytes = aes.IV.Concat(cipherBytes).ToArray();
            var prependedBase64 = Convert.ToBase64String(prependedBytes);
            return prependedBase64;
        }
    }