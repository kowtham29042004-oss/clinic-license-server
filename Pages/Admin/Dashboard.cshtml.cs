using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Pages.Admin
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly LicenseDbContext _db;
        public DashboardModel(LicenseDbContext db) => _db = db;

        public int TotalCustomers   { get; set; }
        public int ActiveLicenses   { get; set; }
        public int ExpiringIn7Days  { get; set; }
        public int RevokedLicenses  { get; set; }
        public List<License> RecentLicenses { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalCustomers  = await _db.Customers.CountAsync();
            ActiveLicenses  = await _db.Licenses.CountAsync(l => l.Status == LicenseStatus.Active);
            RevokedLicenses = await _db.Licenses.CountAsync(l => l.Status == LicenseStatus.Revoked);
            ExpiringIn7Days = await _db.Licenses.CountAsync(l =>
                l.Status == LicenseStatus.Active &&
                l.ExpiresAt >= DateTime.UtcNow &&
                l.ExpiresAt <= DateTime.UtcNow.AddDays(7));

            RecentLicenses = await _db.Licenses
                .Include(l => l.Customer)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();
        }
    }
}
