using System.ComponentModel.DataAnnotations;

namespace LicenseServer.Models
{
    public enum LicenseType { Trial, Monthly, Annual, Lifetime }
    public enum LicenseStatus { Active, Revoked, Suspended, Expired }

    public class Customer
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = "";
        [Required, MaxLength(200)] public string Email { get; set; } = "";
        [MaxLength(50)]  public string Phone { get; set; } = "";
        [MaxLength(500)] public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<License> Licenses { get; set; } = new();
    }

    public class License
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [Required, MaxLength(60)] public string LicenseKey { get; set; } = "";
        public LicenseType Type { get; set; }
        public LicenseStatus Status { get; set; } = LicenseStatus.Active;
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxMachines { get; set; } = 1;
        [MaxLength(2000)] public string Features { get; set; } = ""; // comma-separated
        [MaxLength(500)]  public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<MachineActivation> Activations { get; set; } = new();
    }

    public class MachineActivation
    {
        public int Id { get; set; }
        public int LicenseId { get; set; }
        public License License { get; set; } = null!;

        [Required, MaxLength(128)] public string MachineIdHash { get; set; } = "";
        [MaxLength(200)]           public string MachineName { get; set; } = "";
        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;
    }

    public class AdminUser
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Username { get; set; } = "";
        [Required, MaxLength(300)] public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
