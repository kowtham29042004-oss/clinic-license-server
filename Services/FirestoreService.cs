using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LicenseServer.Services
{
    public class FirestoreService
    {
        public FirestoreDb Db { get; }
        private readonly ILogger<FirestoreService> _log;

        public FirestoreService(IConfiguration config, ILogger<FirestoreService> log)
        {
            _log = log;

            string projectId = config["Firebase:ProjectId"]
                ?? throw new InvalidOperationException("Firebase:ProjectId not configured.");

            // CLOUD (Render): key passed as env variable FIREBASE_SERVICE_ACCOUNT_JSON
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
                // LOCAL DEV: use file path from appsettings.json
                string? filePath = config["Firebase:ServiceAccountKeyPath"];
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);
                    _log.LogInformation("Firebase: using credentials from file {Path}", filePath);
                }
                else
                {
                    _log.LogWarning("Firebase: no credentials found. Set FIREBASE_SERVICE_ACCOUNT_JSON env var.");
                }
            }

            Db = FirestoreDb.Create(projectId);
            _log.LogInformation("Firestore connected → project: {ProjectId}", projectId);
        }
    }
}
