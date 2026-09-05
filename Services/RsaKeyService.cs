using System.Security.Cryptography;

namespace LicenseServer.Services
{
    /// <summary>
    /// Manages the RSA-PSS key pair.
    /// Private key lives ONLY on the server – never in the WPF client.
    /// Public key is embedded in the WPF client for signature verification.
    /// </summary>
    public class RsaKeyService
    {
        private readonly RSA _rsa;
        private readonly ILogger<RsaKeyService> _logger;

        public RsaKeyService(IConfiguration config, ILogger<RsaKeyService> logger)
        {
            _logger = logger;
            _rsa = RSA.Create(4096);

            // First try to load from environment variable (permanent storage)
            string? pemFromEnv = Environment.GetEnvironmentVariable("RSA_PRIVATE_KEY");
            if (!string.IsNullOrEmpty(pemFromEnv))
            {
                _rsa.ImportFromPem(pemFromEnv.Replace("\\n", "\n"));
                _logger.LogInformation("RSA private key loaded from environment variable.");
                return;
            }

            // Fallback: load from file
            string keyPath = config["Licensing:PrivateKeyPath"]
                             ?? Path.Combine(AppContext.BaseDirectory, "keys", "private.pem");
            Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);

            if (File.Exists(keyPath))
            {
                _rsa.ImportFromPem(File.ReadAllText(keyPath));
                _logger.LogInformation("RSA private key loaded from {Path}", keyPath);
            }
            else
            {
                string privatePem = _rsa.ExportRSAPrivateKeyPem();
                File.WriteAllText(keyPath, privatePem);
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                _logger.LogWarning("New RSA key pair generated. Private key saved to {Path}", keyPath);
                string pubPath = Path.Combine(Path.GetDirectoryName(keyPath)!, "public.pem");
                File.WriteAllText(pubPath, _rsa.ExportSubjectPublicKeyInfoPem());
                // Print public key to logs so we can always get it
                _logger.LogWarning("PUBLIC KEY:\n{PubKey}", _rsa.ExportSubjectPublicKeyInfoPem());
            }
        }

        /// <summary>Signs data with RSA-PSS / SHA-256.</summary>
        public byte[] Sign(byte[] data)
        {
            return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }

        /// <summary>Returns the public key as PEM – safe to embed in WPF client.</summary>
        public string GetPublicKeyPem()
        {
            return _rsa.ExportSubjectPublicKeyInfoPem();
        }

        /// <summary>Generates a new RSA key pair and saves it (for key rotation).</summary>
        public (string privatePem, string publicPem) GenerateNewKeyPair()
        {
            using var rsa = RSA.Create(4096);
            return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
        }
    }
}
