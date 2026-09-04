using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Data
{
    public class LicenseDbContext : DbContext
    {
        public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<License> Licenses => Set<License>();
        public DbSet<MachineActivation> MachineActivations => Set<MachineActivation>();
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<License>()
                .HasIndex(l => l.LicenseKey)
                .IsUnique();

            b.Entity<MachineActivation>()
                .HasIndex(m => new { m.LicenseId, m.MachineIdHash })
                .IsUnique();
        }
    }
}
