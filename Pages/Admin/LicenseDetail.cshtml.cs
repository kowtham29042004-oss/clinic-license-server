using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Pages.Admin
{
    [Authorize]
    public class LicenseDetailModel : PageModel
    {
        private readonly LicenseDbContext _db;
        public LicenseDetailModel(LicenseDbContext db) => _db = db;

        public License? License { get; set; }
        public string  Message  { get; set; } = "";
        public string  Error    { get; set; } = "";

        private async Task LoadAsync(int id)
        {
            License = await _db.Licenses
                .Include(l => l.Customer)
                .Include(l => l.Activations)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task OnGetAsync(int id) => await LoadAsync(id);

        public async Task<IActionResult> OnPostExtendAsync(int id, string newExpiry)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();

            if (!DateTime.TryParse(newExpiry, out DateTime dt))
            {
                Error = "Invalid date.";
                await LoadAsync(id);
                return Page();
            }

            l.ExpiresAt = dt.Date.AddDays(1).AddSeconds(-1);
            l.Status    = LicenseStatus.Active;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            Message = $"License extended to {dt:dd MMM yyyy}.";
            await LoadAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostRevokeAsync(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Revoked;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            Message = "License revoked.";
            await LoadAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostSuspendAsync(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Suspended;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            Message = "License suspended.";
            await LoadAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            var l = await _db.Licenses.FindAsync(id);
            if (l == null) return NotFound();
            l.Status    = LicenseStatus.Active;
            l.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            Message = "License reactivated.";
            await LoadAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostResetMachinesAsync(int id)
        {
            var machines = await _db.MachineActivations
                .Where(m => m.LicenseId == id && !m.IsRevoked)
                .ToListAsync();
            foreach (var m in machines) m.IsRevoked = true;
            await _db.SaveChangesAsync();
            Message = $"Reset {machines.Count} machine(s).";
            await LoadAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostRevokeMachineAsync(int id, int machineId)
        {
            var m = await _db.MachineActivations
                .FirstOrDefaultAsync(m => m.Id == machineId && m.LicenseId == id);
            if (m != null) { m.IsRevoked = true; await _db.SaveChangesAsync(); }
            Message = "Machine slot released.";
            await LoadAsync(id);
            return Page();
        }
    }
}
