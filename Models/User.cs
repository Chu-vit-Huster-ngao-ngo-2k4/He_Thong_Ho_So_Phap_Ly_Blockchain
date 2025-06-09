using System.ComponentModel.DataAnnotations;

namespace prj1.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Họ tên")]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public required string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Vai trò")]
        public required string Role { get; set; } = "User"; // Default role

        public ICollection<LegalProfile> LegalProfiles { get; set; } = new List<LegalProfile>();
        public ICollection<LegalProfilePermission> Permissions { get; set; } = new List<LegalProfilePermission>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
} 