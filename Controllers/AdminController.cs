using LicenseServer.Data;
using LicenseServer.Models;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LicenseServer.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly LicenseDbContext _db;

        public AdminController(LicenseDbContext db) => _db = db;

        // ── CUSTOMERS ─────────────────────────────────────────────────────

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _db.Customers
                .Include(c => c.Licenses)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(customers.Select(c => new
            {
                c.Id, c.Name, c.Email, c.Phone, c.Notes, c.CreatedAt,
                LicenseCount = c.Licenses.Count
            }));
        }

        [HttpGet("customers/{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var c = await _db.Customers
                .Include(c => c.Licenses)
                .ThenInclude(l => l.Activations)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPost("customers")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var c = new Customer { Name = req.Name, Email = req.Email, Phone = req.Phone, Notes = req.Notes ?? "" };
            _db.Customers.Add(c);
            await _db.SaveChangesAsync();
            return Ok(new { c.Id, c.Name, c.Email });
        }

        [HttpPut("customers/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerRequest req)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return NotFound();
            c.Name = req.Name; c.Email = req.Email;
            c.Phone = req.Phone; c.Notes = req.Notes ?? "";
            await _db.SaveChangesAsync();
            return Ok(c);
        }

        // ── LICENSES ──────────────────────────────────────────────────────

        [HttpGet("licenses")]
        public async Task<IActionResult> GetLicenses()
        {
            var list = await _db.Licenses
                .Include(l => l.Customer)
                .Include(l => l.Activations)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Ok(list.Select(l => new
            {
                l.Id, l.LicenseKey, l.Type, l.Status,
                l.StartsAt, l.ExpiresAt, l.MaxMachines, l.Features, l.Notes, l.CreatedAt,
                CustomerName = l.Customer?.Name,
                ActiveMachines = l.Activations.Count(a => !a.IsRevoked)
            }));
        }

        [HttpGet("licenses/{id}")]
        public async Task<IActionResult> GetLicense(int id)
        {
            var l = await _db.Licenses
                .Include(l => l.Customer)
                .Include(l => l.Activations)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (l == null) return NotFound();
            return Ok(l);
        }

        [HttpPost("licenses")]
        public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customer = await _db.Customers.FindAsync(req.CustomerId);
            if (customer == null) return BadRequest(new { error = "Customer not found." });

            string key = LicenseKeyGenerator.Generate();
            // Ensure uniqueness (astronomically unlikely to collide)
            while (await _db.Licenses.AnyAsync(l => l.LicenseKey == key))
                key = LicenseKeyGenerator.Generate();

            var license = new License
            {
                CustomerId  = req.CustomerId,
                LicenseKey  = key,
                Type        = req.Type,
                Status      = LicenseStatus.Active,
                StartsAt    = req.StartsAt.Date,
                ExpiresAt   = req.ExpiresAt.Date.AddDays(1).AddSeconds(-1), // end of that day UTC
                MaxMachines = req.MaxMachines,
                Features    = req.Features ?? "",
                Notes       = req.Notes ?? ""
            };

            _db.Licenses.Add(license);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                license.Id,
                license.LicenseKey,
                license.Type,
                license.StartsAt,
                license.ExpiresAt,
                license.MaxMachines,
                CustomerName = customer.Name
            });
        }

        [HttpPut("licenses/{id}/extend")]
        public async Task<IActionResult> ExtendLicense(int id, [FromBody] ExtendRequest req)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.ExpiresAt  = req.NewExpiresAt.Date.AddDays(1).AddSeconds(-1);
            l.Status     = LicenseStatus.Active; // reactivate if was expired
            l.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { l.Id, l.ExpiresAt });
        }

        [HttpPut("licenses/{id}/revoke")]
        public async Task<IActionResult> RevokeLicense(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Revoked;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { l.Id, l.Status });
        }

        [HttpPut("licenses/{id}/suspend")]
        public async Task<IActionResult> SuspendLicense(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Suspended;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { l.Id, l.Status });
        }

        [HttpPut("licenses/{id}/activate")]
        public async Task<IActionResult> ReactivateLicense(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Active;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { l.Id, l.Status });
        }

        // ── MACHINE ACTIVATIONS ───────────────────────────────────────────

        [HttpGet("licenses/{id}/machines")]
        public async Task<IActionResult> GetMachines(int id)
        {
            var machines = await _db.MachineActivations
                .Where(m => m.LicenseId == id)
                .OrderByDescending(m => m.ActivatedAt)
                .ToListAsync();
            return Ok(machines);
        }

        [HttpDelete("licenses/{licenseId}/machines/{machineId}")]
        public async Task<IActionResult> RevokeOneMachine(int licenseId, int machineId)
        {
            var m = await _db.MachineActivations
                .FirstOrDefaultAsync(m => m.Id == machineId && m.LicenseId == licenseId);
            if (m == null) return NotFound();
            m.IsRevoked = true;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Machine slot released." });
        }

        [HttpDelete("licenses/{id}/machines")]
        public async Task<IActionResult> ResetAllMachines(int id)
        {
            var machines = await _db.MachineActivations
                .Where(m => m.LicenseId == id && !m.IsRevoked)
                .ToListAsync();
            foreach (var m in machines) m.IsRevoked = true;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Reset {machines.Count} machine(s)." });
        }

        // ── PUBLIC KEY ────────────────────────────────────────────────────

        [HttpGet("publickey")]
        [AllowAnonymous] // Safe to expose – this is the PUBLIC key only
        public IActionResult GetPublicKey([FromServices] RsaKeyService keys)
        {
            return Ok(new { publicKeyPem = keys.GetPublicKeyPem() });
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────

    public class CreateCustomerRequest
    {
        [Required] public string Name  { get; set; } = "";
        [Required] public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Notes { get; set; }
    }

    public class CreateLicenseRequest
    {
        [Required] public int CustomerId  { get; set; }
        public LicenseType Type           { get; set; } = LicenseType.Annual;
        [Required] public DateTime StartsAt  { get; set; }
        [Required] public DateTime ExpiresAt { get; set; }
        [Range(1, 100)] public int MaxMachines { get; set; } = 1;
        public string? Features { get; set; }
        public string? Notes    { get; set; }
    }

    public class ExtendRequest
    {
        [Required] public DateTime NewExpiresAt { get; set; }
    }
}
