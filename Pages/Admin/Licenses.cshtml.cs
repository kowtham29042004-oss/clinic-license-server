using LicenseServer.Data;
using LicenseServer.Models;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Pages.Admin
{
    [Authorize]
    public class LicensesModel : PageModel
    {
        private readonly LicenseDbContext _db;
        public LicensesModel(LicenseDbContext db) => _db = db;

        public List<License>  Licenses  { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public string?  Message { get; set; }
        public string?  NewKey  { get; set; }

        [BindProperty] public LicenseInput Input { get; set; } = new();

        public async Task OnGetAsync(int? customerId = null)
        {
            Customers = await _db.Customers.OrderBy(c => c.Name).ToListAsync();

            var q = _db.Licenses.Include(l => l.Customer).Include(l => l.Activations).AsQueryable();
            if (customerId.HasValue) q = q.Where(l => l.CustomerId == customerId);
            Licenses = await q.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            string key = LicenseKeyGenerator.Generate();
            while (await _db.Licenses.AnyAsync(l => l.LicenseKey == key))
                key = LicenseKeyGenerator.Generate();

            if (!Enum.TryParse<LicenseType>(Input.Type, out var licType))
                licType = LicenseType.Annual;

            var license = new License
            {
                CustomerId   = Input.CustomerId,
                LicenseKey   = key,
                Type         = licType,
                Status       = LicenseStatus.Active,
                StartsAt     = Input.StartsAt,
                ExpiresAt    = Input.ExpiresAt.Date.AddDays(1).AddSeconds(-1),
                MaxMachines  = Input.MaxMachines,
                Features     = Input.Features ?? "",
                Notes        = Input.Notes ?? ""
            };

            _db.Licenses.Add(license);
            await _db.SaveChangesAsync();

            NewKey  = key;
            Message = "License created successfully.";
            await OnGetAsync();
            return Page();
        }
    }

    public class LicenseInput
    {
        public int      CustomerId  { get; set; }
        public string   Type        { get; set; } = "Annual";
        public int      MaxMachines { get; set; } = 1;
        public DateTime StartsAt    { get; set; } = DateTime.Today;
        public DateTime ExpiresAt   { get; set; } = DateTime.Today.AddYears(1);
        public string?  Features    { get; set; }
        public string?  Notes       { get; set; }
    }
}
