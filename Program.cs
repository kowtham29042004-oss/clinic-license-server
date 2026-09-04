using AspNetCoreRateLimit;
using LicenseServer.Data;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Use PORT env var from Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── SQLite (admin panel accounts) ────────────────────────────────
builder.Services.AddDbContext<LicenseDbContext>(opts =>
    opts.UseSqlite("Data Source=/tmp/license-admin.db"));

// ── Firebase Firestore ────────────────────────────────────────────
builder.Services.AddSingleton<FirestoreService>();

// ── RSA signing ───────────────────────────────────────────────────
builder.Services.AddSingleton<RsaKeyService>();
builder.Services.AddSingleton<LicenseTokenService>();

// ── Rate limiting ─────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── Cookie auth ───────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath       = "/Auth/Login";
        o.LogoutPath      = "/Auth/Logout";
        o.ExpireTimeSpan  = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("AdminOnly", p => p.RequireClaim("role", "admin")));

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(opt => opt.AddPolicy("Netlify", p =>
    p.WithOrigins("https://clinic-admin-2026.netlify.app", "http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate SQLite
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

app.Run();
