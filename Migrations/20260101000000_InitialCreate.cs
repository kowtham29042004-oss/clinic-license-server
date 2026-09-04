using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicenseServer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Username     = table.Column<string>(maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(maxLength: 300, nullable: false),
                    CreatedAt    = table.Column<DateTime>(nullable: false)
                },
                constraints: t => t.PrimaryKey("PK_AdminUsers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name      = table.Column<string>(maxLength: 200, nullable: false),
                    Email     = table.Column<string>(maxLength: 200, nullable: false),
                    Phone     = table.Column<string>(maxLength: 50,  nullable: false, defaultValue: ""),
                    Notes     = table.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: t => t.PrimaryKey("PK_Customers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CustomerId  = table.Column<int>(nullable: false),
                    LicenseKey  = table.Column<string>(maxLength: 60,   nullable: false),
                    Type        = table.Column<int>(nullable: false),
                    Status      = table.Column<int>(nullable: false),
                    StartsAt    = table.Column<DateTime>(nullable: false),
                    ExpiresAt   = table.Column<DateTime>(nullable: false),
                    MaxMachines = table.Column<int>(nullable: false),
                    Features    = table.Column<string>(maxLength: 2000, nullable: false, defaultValue: ""),
                    Notes       = table.Column<string>(maxLength: 500,  nullable: false, defaultValue: ""),
                    CreatedAt   = table.Column<DateTime>(nullable: false),
                    UpdatedAt   = table.Column<DateTime>(nullable: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Licenses", x => x.Id);
                    t.ForeignKey("FK_Licenses_Customers_CustomerId", x => x.CustomerId,
                        principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MachineActivations",
                columns: table => new
                {
                    Id            = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    LicenseId     = table.Column<int>(nullable: false),
                    MachineIdHash = table.Column<string>(maxLength: 128, nullable: false),
                    MachineName   = table.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
                    ActivatedAt   = table.Column<DateTime>(nullable: false),
                    LastSeenAt    = table.Column<DateTime>(nullable: false),
                    IsRevoked     = table.Column<bool>(nullable: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_MachineActivations", x => x.Id);
                    t.ForeignKey("FK_MachineActivations_Licenses_LicenseId", x => x.LicenseId,
                        principalTable: "Licenses", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Licenses_LicenseKey",       "Licenses",           "LicenseKey", unique: true);
            migrationBuilder.CreateIndex("IX_Licenses_CustomerId",        "Licenses",           "CustomerId");
            migrationBuilder.CreateIndex("IX_MachineActivations_LicenseId_MachineIdHash",
                "MachineActivations", new[] { "LicenseId", "MachineIdHash" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("MachineActivations");
            migrationBuilder.DropTable("Licenses");
            migrationBuilder.DropTable("Customers");
            migrationBuilder.DropTable("AdminUsers");
        }
    }
}
