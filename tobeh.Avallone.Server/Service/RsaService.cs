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
                File.WriteAllText(PrivateKeyPath, _rsa.ExportRSAPrivateKeyPem());
                File.WriteAllText(PublicKeyPath, _rsa.ExportRSAPublicKeyPem());
            }
            else if (_rsa == null)
            {
                /* load from path */
                _rsa = RSA.Create();
                _rsa.ImportFromPem(File.ReadAllText(PrivateKeyPath));
                _rsa.ImportFromPem(File.ReadAllText(PublicKeyPath));
            }
        }
        finally
        {
            KeySemaphore.Release();
        }

        return _rsa;
    }
}