using LicenseServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicenseServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivationController : ControllerBase
    {
        private readonly FirestoreService    _firestore;
        private readonly LicenseTokenService _tokenSvc;
        private readonly ILogger<ActivationController> _log;

        public ActivationController(
            FirestoreService firestore,
            LicenseTokenService tokenSvc,
            ILogger<ActivationController> log)
        {
            _firestore = firestore;
            _tokenSvc  = tokenSvc;
            _log       = log;
        }

        // ── POST /api/activation/activate ─────────────────────────────────
        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.LicenseKey) ||
                string.IsNullOrWhiteSpace(req.MachineIdHash))
                return BadRequest(new { error = "LicenseKey and MachineIdHash are required." });

            string key = req.LicenseKey.Trim().ToUpperInvariant();

            // ── Read from Firebase ────────────────────────────────────────
            FirestoreLicense? lic;
            try { lic = await _firestore.GetLicenseByKeyAsync(key); }
            catch (Exception ex)
            {
                _log.LogError(ex, "Firestore read failed");
                return StatusCode(503, new { error = "License server temporarily unavailable." });
            }

            if (lic == null)
                return NotFound(new { error = "Invalid license key." });

            // ── Validate status ───────────────────────────────────────────
            if (lic.Status == "Revoked")
                return StatusCode(403, new { error = "License has been revoked." });

            if (lic.Status == "Suspended")
                return StatusCode(403, new { error = "License is suspended. Contact support." });

            if (lic.Type != "Lifetime" && DateTime.UtcNow > lic.ExpiresAt)
                return StatusCode(402, new { error = "License has expired." });

            if (DateTime.UtcNow < lic.StartsAt)
                return StatusCode(403, new { error = "License is not yet valid." });

            // ── Machine binding ───────────────────────────────────────────
            string machineHash = req.MachineIdHash.ToLowerInvariant();

            var existingMachine = lic.Activations
                .FirstOrDefault(a => a.MachineIdHash == machineHash && !a.IsRevoked);

            if (existingMachine == null)
            {
                int activeCount = lic.Activations.Count(a => !a.IsRevoked);
                if (activeCount >= lic.MaxMachines)
                {
                    _log.LogWarning("License {Key} exceeded max machines ({Max})", key, lic.MaxMachines);
                    return StatusCode(403, new
                    {
                        error = $"Maximum machine limit ({lic.MaxMachines}) reached. " +
                                "Contact your administrator to reset a machine slot."
                    });
                }

                // Register new machine
                lic.Activations.Add(new FirestoreMachineActivation
                {
                    MachineIdHash = machineHash,
                    MachineName   = req.MachineName ?? "",
                    ActivatedAt   = DateTime.UtcNow,
                    LastSeenAt    = DateTime.UtcNow,
                    IsRevoked     = false
                });
                _log.LogInformation("New machine registered for {Key}: {Hash}", key, machineHash[..8]);
            }
            else
            {
                existingMachine.LastSeenAt = DateTime.UtcNow;
            }

            // ── Save activations back to Firebase ─────────────────────────
            await _firestore.UpdateActivationsAsync(lic.DocId, lic.Activations);

            // ── Issue RSA-signed token ────────────────────────────────────
            var token = _tokenSvc.CreateToken(lic);
            return Ok(token);
        }

        // ── POST /api/activation/heartbeat ────────────────────────────────
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.LicenseKey) ||
                string.IsNullOrWhiteSpace(req.MachineIdHash))
                return BadRequest(new { error = "Missing fields." });

            string key         = req.LicenseKey.Trim().ToUpperInvariant();
            string machineHash = req.MachineIdHash.ToLowerInvariant();

            FirestoreLicense? lic;
            try { lic = await _firestore.GetLicenseByKeyAsync(key); }
            catch { return StatusCode(503, new { error = "Server unavailable." }); }

            if (lic == null)
                return NotFound(new { error = "Invalid license key." });

            if (lic.Status == "Revoked")
                return StatusCode(403, new { error = "License revoked." });

            if (lic.Status == "Suspended")
                return StatusCode(403, new { error = "License suspended." });

            if (lic.Type != "Lifetime" && DateTime.UtcNow > lic.ExpiresAt)
                return StatusCode(402, new { error = "License expired." });

            var machine = lic.Activations
                .FirstOrDefault(a => a.MachineIdHash == machineHash && !a.IsRevoked);

            if (machine == null)
                return StatusCode(403, new { error = "Machine not registered." });

            machine.LastSeenAt = DateTime.UtcNow;
            await _firestore.UpdateActivationsAsync(lic.DocId, lic.Activations);

            return Ok(_tokenSvc.CreateToken(lic));
        }
    }

    public class ActivateRequest
    {
        public string LicenseKey    { get; set; } = "";
        public string MachineIdHash { get; set; } = "";
        public string MachineName   { get; set; } = "";
    }

    public class HeartbeatRequest
    {
        public string LicenseKey    { get; set; } = "";
        public string MachineIdHash { get; set; } = "";
    }
}
