using LicenseServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LicenseServer.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly LicenseDbContext _db;
        public LoginModel(LicenseDbContext db) => _db = db;

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        public string Error { get; set; } = "";

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Admin/Dashboard");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _db.AdminUsers
                .FirstOrDefaultAsync(u => u.Username == Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                Error = "Invalid username or password.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, "Admin")
            };
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
