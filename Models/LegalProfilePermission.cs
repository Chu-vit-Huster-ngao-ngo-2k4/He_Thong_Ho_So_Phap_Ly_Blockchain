using System.ComponentModel.DataAnnotations;

namespace prj1.Models
{
    public class LegalProfilePermission
    {
        public int Id { get; set; }

        public int LegalProfileId { get; set; }
        public LegalProfile LegalProfile { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string PermissionType { get; set; } = "View"; // View, Edit, DownloadOnly
    }
} 