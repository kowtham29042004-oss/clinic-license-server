using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicenseServer.Services
{
    public class LicenseTokenService
    {
        private readonly RsaKeyService _keys;
        public LicenseTokenService(RsaKeyService keys) => _keys = keys;

        /// <summary>Creates RSA-PSS signed token from a Firestore license.</summary>
        public SignedLicenseToken CreateToken(FirestoreLicense license)
        {
            var payload = new LicensePayload
            {
                LicenseKey    = license.LicenseKey,
                CustomerName  = license.CustomerName,
                MachineIdHash = "", // filled by caller via overload
                Type          = license.Type,
                Status        = license.Status,
                StartsAt      = license.StartsAt,
                ExpiresAt     = license.ExpiresAt,
                MaxMachines   = license.MaxMachines,
                Features      = license.Features,
                IssuedAt      = DateTime.UtcNow,
                ServerUtcNow  = DateTime.UtcNow
            };
            return Sign(payload);
        }

        /// <summary>Creates RSA-PSS signed token with machine hash.</summary>
        public SignedLicenseToken CreateToken(FirestoreLicense license, string machineIdHash)
        {
            var payload = new LicensePayload
            {
                LicenseKey    = license.LicenseKey,
                CustomerName  = license.CustomerName,
                MachineIdHash = machineIdHash,
                Type          = license.Type,
                Status        = license.Status,
                StartsAt      = license.StartsAt,
                ExpiresAt     = license.ExpiresAt,
                MaxMachines   = license.MaxMachines,
                Features      = license.Features,
                IssuedAt      = DateTime.UtcNow,
                ServerUtcNow  = DateTime.UtcNow
            };
            return Sign(payload);
        }

        private SignedLicenseToken Sign(LicensePayload payload)
        {
            string json         = JsonSerializer.Serialize(payload);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(json);
            byte[] signature    = _keys.Sign(payloadBytes);

            return new SignedLicenseToken
            {
                Payload   = Convert.ToBase64String(payloadBytes),
                Signature = Convert.ToBase64String(signature)
            };
        }
    }

    public class LicensePayload
    {
        public string   LicenseKey    { get; set; } = "";
        public string   CustomerName  { get; set; } = "";
        public string   MachineIdHash { get; set; } = "";
        public string   Type          { get; set; } = "";
        public string   Status        { get; set; } = "";
        public DateTime StartsAt      { get; set; }
        public DateTime ExpiresAt     { get; set; }
        public int      MaxMachines   { get; set; }
        public string   Features      { get; set; } = "";
        public DateTime IssuedAt      { get; set; }
        public DateTime ServerUtcNow  { get; set; }
    }

    public class SignedLicenseToken
    {
        public string Payload   { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}
