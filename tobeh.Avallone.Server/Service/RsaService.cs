using System.Security.Cryptography;

namespace tobeh.Avallone.Server.Service;

public class RsaService
{
    private const string PrivateKeyPath = "/rsa/private.pem";
    private const string PublicKeyPath = "/rsa/public.pem";
    private static readonly SemaphoreSlim KeySemaphore = new(1, 1);
    
    private RSA? _rsa;
    
    public RSA GetRsa()
    {
        if (_rsa != null) return _rsa;
        
        KeySemaphore.Wait();
        try
        {
            if(!File.Exists(PrivateKeyPath) || !File.Exists(PublicKeyPath))
            {
                _rsa = RSA.Create(512);
                File.WriteAllBytes(PrivateKeyPath, _rsa.ExportRSAPrivateKey());
                File.WriteAllBytes(PublicKeyPath, _rsa.ExportRSAPublicKey());
            }
            else if (_rsa == null)
            {
                /* load from path */
                _rsa = RSA.Create();
                _rsa.ImportRSAPrivateKey(File.ReadAllBytes(PrivateKeyPath), out _);
                _rsa.ImportRSAPublicKey(File.ReadAllBytes(PublicKeyPath), out _);
            }
        }
        finally
        {
            KeySemaphore.Release();
        }

        return _rsa;
    }
}