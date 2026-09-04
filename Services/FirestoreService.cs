using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseServer.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _db;
        private readonly ILogger<FirestoreService> _log;

        public FirestoreService(IConfiguration config, ILogger<FirestoreService> log)
        {
            _log = log;
            string projectId = config["Firebase:ProjectId"]
                ?? throw new InvalidOperationException("Firebase:ProjectId not set.");

            // CLOUD (Render): credentials passed as env variable
            string? jsonEnv = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON");
            if (!string.IsNullOrWhiteSpace(jsonEnv))
            {
                string tmp = Path.Combine(Path.GetTempPath(), "firebase-sa.json");
                File.WriteAllText(tmp, jsonEnv);
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tmp);
                _log.LogInformation("Firebase: using credentials from environment variable.");
            }
            else
            {
                // LOCAL DEV: use file path
                string? credPath = config["Firebase:ServiceAccountKeyPath"];
                if (!string.IsNullOrEmpty(credPath) && File.Exists(credPath))
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credPath);
                    _log.LogInformation("Firebase: using credentials from file {Path}", credPath);
                }
                else
                {
                    _log.LogWarning("Firebase: no credentials found!");
                }
            }

            _db = FirestoreDb.Create(projectId);
            _log.LogInformation("Firestore connected to project: {ProjectId}", projectId);
        }

        // ── GET LICENSE BY KEY ──────────────────────────────────────────────
        public async Task<FirestoreLicense?> GetLicenseByKeyAsync(string licenseKey)
        {
            try
            {
                var query = _db.Collection("licenses")
                    .WhereEqualTo("licenseKey", licenseKey.ToUpperInvariant());
                var snap = await query.GetSnapshotAsync();

                if (snap.Count == 0) return null;

                var doc  = snap.Documents[0];
                var data = doc.ToDictionary();

                return new FirestoreLicense
                {
                    DocId         = doc.Id,
                    LicenseKey    = data.GetStr("licenseKey"),
                    CustomerName  = data.GetStr("customerName"),
                    CustomerEmail = data.GetStr("customerEmail"),
                    Type          = data.GetStr("type"),
                    Status        = data.GetStr("status"),
                    MaxMachines   = Convert.ToInt32(data.GetValueOrDefault("maxMachines", 1)),
                    Features      = data.GetStr("features"),
                    StartsAt      = data.GetDateTime("startsAt"),
                    ExpiresAt     = data.GetDateTime("expiresAt"),
                    Activations   = data.GetActivations()
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Firestore GetLicenseByKey failed for key: {Key}", licenseKey);
                throw;
            }
        }

        // ── UPDATE ACTIVATIONS ──────────────────────────────────────────────
        public async Task UpdateActivationsAsync(string docId,
            List<FirestoreMachineActivation> activations)
        {
            var docRef = _db.Collection("licenses").Document(docId);
            var list   = activations.Select(a => new Dictionary<string, object>
            {
                ["machineIdHash"] = a.MachineIdHash,
                ["machineName"]   = a.MachineName,
                ["activatedAt"]   = a.ActivatedAt.ToString("O"),
                ["lastSeenAt"]    = a.LastSeenAt.ToString("O"),
                ["isRevoked"]     = a.IsRevoked
            }).ToList<object>();

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                ["activations"] = list,
                ["updatedAt"]   = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }
    }

    // ── Models ──────────────────────────────────────────────────────────────
    public class FirestoreLicense
    {
        public string   DocId         { get; set; } = "";
        public string   LicenseKey    { get; set; } = "";
        public string   CustomerName  { get; set; } = "";
        public string   CustomerEmail { get; set; } = "";
        public string   Type          { get; set; } = "";
        public string   Status        { get; set; } = "";
        public int      MaxMachines   { get; set; } = 1;
        public string   Features      { get; set; } = "";
        public DateTime StartsAt      { get; set; }
        public DateTime ExpiresAt     { get; set; }
        public List<FirestoreMachineActivation> Activations { get; set; } = new();
    }

    public class FirestoreMachineActivation
    {
        public string   MachineIdHash { get; set; } = "";
        public string   MachineName   { get; set; } = "";
        public DateTime ActivatedAt   { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt    { get; set; } = DateTime.UtcNow;
        public bool     IsRevoked     { get; set; } = false;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    internal static class DictExtensions
    {
        public static string GetStr(this Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        public static DateTime GetDateTime(this Dictionary<string, object> d, string key)
        {
            if (!d.TryGetValue(key, out var v)) return DateTime.MaxValue;
            if (v is Timestamp ts) return ts.ToDateTime();
            if (DateTime.TryParse(v?.ToString(), out var dt)) return dt;
            return DateTime.MaxValue;
        }

        public static List<FirestoreMachineActivation> GetActivations(
            this Dictionary<string, object> d)
        {
            var result = new List<FirestoreMachineActivation>();
            if (!d.TryGetValue("activations", out var raw)) return result;
            if (raw is not IEnumerable<object> items) return result;
            foreach (var item in items)
            {
                if (item is not Dictionary<string, object> a) continue;
                result.Add(new FirestoreMachineActivation
                {
                    MachineIdHash = a.GetStr("machineIdHash"),
                    MachineName   = a.GetStr("machineName"),
                    IsRevoked     = a.TryGetValue("isRevoked", out var rev) && rev is bool b && b,
                    ActivatedAt   = DateTime.TryParse(a.GetStr("activatedAt"), out var at) ? at : DateTime.UtcNow,
                    LastSeenAt    = DateTime.TryParse(a.GetStr("lastSeenAt"), out var ls)  ? ls : DateTime.UtcNow
                });
            }
            return result;
        }
    }
}
