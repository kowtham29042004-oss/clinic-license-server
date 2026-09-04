using AspNetCoreRateLimit;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ── Firebase Firestore (replaces SQLite completely) ────────────────
builder.Services.AddSingleton<FirestoreService>();

// ── RSA key service (private key stays on server ONLY) ────────────
builder.Services.AddSingleton<RsaKeyService>();
builder.Services.AddSingleton<LicenseTokenService>();

// ── Rate limiting ──────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── MVC + Razor Pages ─────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── CORS (allow your Netlify site to call the API if needed) ──────
builder.Services.AddCors(opt => opt.AddPolicy("Netlify", p =>
    p.WithOrigins("https://clinic-admin-2026.netlify.app",
                  "http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseHttpsRedirection();
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
