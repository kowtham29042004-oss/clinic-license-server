using AspNetCoreRateLimit;
using LicenseServer.Data;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── SQLite (for admin web panel user accounts only) ───────────────
builder.Services.AddDbContext<LicenseDbContext>(opts =>
    opts.UseSqlite("Data Source=license-admin.db"));

// ── Firebase Firestore (license storage) ─────────────────────────
builder.Services.AddSingleton<FirestoreService>();

// ── RSA key service ───────────────────────────────────────────────
builder.Services.AddSingleton<RsaKeyService>();
builder.Services.AddSingleton<LicenseTokenService>();

// ── Rate limiting ─────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── Cookie auth (admin panel login) ──────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath  = "/Auth/Login";
        o.LogoutPath = "/Auth/Logout";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("AdminOnly", p => p.RequireClaim("role", "admin")));

// ── MVC + Razor Pages ─────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── CORS ──────────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("Netlify", p =>
    p.WithOrigins("https://clinic-admin-2026.netlify.app",
                  "http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto-create SQLite DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.EnsureCreated();
}

app.UseIpRateLimiting();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Netlify");
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/Auth/Login"));

app.Run();
