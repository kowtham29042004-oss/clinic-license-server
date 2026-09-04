using LicenseServer.Data;
using LicenseServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Pages.Admin
{
    [Authorize]
    public class CustomersModel : PageModel
    {
        private readonly LicenseDbContext _db;
        public CustomersModel(LicenseDbContext db) => _db = db;

        public List<Customer> Customers { get; set; } = new();
        public string Message { get; set; } = "";

        [BindProperty]
        public CustomerInput Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            Customers = await _db.Customers
                .Include(c => c.Licenses)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            _db.Customers.Add(new Customer
            {
                Name = Input.Name, Email = Input.Email,
                Phone = Input.Phone ?? "", Notes = Input.Notes ?? ""
            });
            await _db.SaveChangesAsync();
            Message = $"Customer '{Input.Name}' created.";
            await OnGetAsync();
            return Page();
        }
    }

    public class CustomerInput
    {
        public string Name  { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Notes { get; set; }
    }
}
