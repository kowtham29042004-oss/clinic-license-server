using System;
using LicenseServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable
#pragma warning disable CS8981

namespace LicenseServer.Migrations
{
    [DbContext(typeof(LicenseDbContext))]
    partial class LicenseDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder b)
        {
            b.HasAnnotation("ProductVersion", "8.0.0");

            b.Entity("LicenseServer.Models.AdminUser", e =>
            {
                e.HasKey("Id");
                e.ToTable("AdminUsers");
                e.Property<int>("Id").ValueGeneratedOnAdd();
                e.Property<string>("Username").IsRequired().HasMaxLength(100);
                e.Property<string>("PasswordHash").IsRequired().HasMaxLength(300);
                e.Property<DateTime>("CreatedAt");
            });

            b.Entity("LicenseServer.Models.Customer", e =>
            {
                e.HasKey("Id");
                e.ToTable("Customers");
                e.Property<int>("Id").ValueGeneratedOnAdd();
                e.Property<string>("Name").IsRequired().HasMaxLength(200);
                e.Property<string>("Email").IsRequired().HasMaxLength(200);
                e.Property<string>("Phone").IsRequired().HasMaxLength(50);
                e.Property<string>("Notes").IsRequired().HasMaxLength(500);
                e.Property<DateTime>("CreatedAt");
            });

            b.Entity("LicenseServer.Models.License", e =>
            {
                e.HasKey("Id");
                e.ToTable("Licenses");
                e.HasIndex("LicenseKey").IsUnique();
                e.HasIndex("CustomerId");
                e.Property<int>("Id").ValueGeneratedOnAdd();
                e.Property<int>("CustomerId");
                e.Property<string>("LicenseKey").IsRequired().HasMaxLength(60);
                e.Property<int>("Type");
                e.Property<int>("Status");
                e.Property<DateTime>("StartsAt");
                e.Property<DateTime>("ExpiresAt");
                e.Property<int>("MaxMachines");
                e.Property<string>("Features").IsRequired().HasMaxLength(2000);
                e.Property<string>("Notes").IsRequired().HasMaxLength(500);
                e.Property<DateTime>("CreatedAt");
                e.Property<DateTime>("UpdatedAt");
                e.HasOne("LicenseServer.Models.Customer", "Customer")
                 .WithMany("Licenses").HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity("LicenseServer.Models.MachineActivation", e =>
            {
                e.HasKey("Id");
                e.ToTable("MachineActivations");
                e.HasIndex(new[] { "LicenseId", "MachineIdHash" }).IsUnique();
                e.Property<int>("Id").ValueGeneratedOnAdd();
                e.Property<int>("LicenseId");
                e.Property<string>("MachineIdHash").IsRequired().HasMaxLength(128);
                e.Property<string>("MachineName").IsRequired().HasMaxLength(200);
                e.Property<DateTime>("ActivatedAt");
                e.Property<DateTime>("LastSeenAt");
                e.Property<bool>("IsRevoked");
                e.HasOne("LicenseServer.Models.License", "License")
                 .WithMany("Activations").HasForeignKey("LicenseId").OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
