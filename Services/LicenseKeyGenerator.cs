using System.Security.Cryptography;
using System.Text;

namespace LicenseServer.Services
{
    /// <summary>
    /// Generates cryptographically secure license keys.
    /// Format: XXXX-XXXX-XXXX-XXXX-XXXX (Base32-like, no ambiguous chars)
    /// </summary>
    public static class LicenseKeyGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0,O,1,I

        public static string Generate()
        {
            var sb = new StringBuilder();
            byte[] buf = new byte[25];
            RandomNumberGenerator.Fill(buf);

            for (int i = 0; i < 25; i++)
            {
                if (i > 0 && i % 5 == 0) sb.Append('-');
                sb.Append(Alphabet[buf[i] % Alphabet.Length]);
            }
            // Result: AAAAA-BBBBB-CCCCC-DDDDD-EEEEE  (25 chars + 4 dashes = 29)
            return sb.ToString();
        }
    }
}
