using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace prj1.Models
{
    public class LegalProfile
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên hồ sơ")]
        public required string Name { get; set; }

        [Required]
        [Display(Name = "Mô tả")]
        public required string Description { get; set; }

        [Display(Name = "Danh sách file đính kèm")]
        public ICollection<LegalProfileFile> Files { get; set; } = new List<LegalProfileFile>();

        [Display(Name = "Thời gian tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Trạng thái")]
        public LegalProfileStatus Status { get; set; }

        [Required]
        [Display(Name = "Người tạo")]
        public int UserId { get; set; }
        public User? User { get; set; }

        public ICollection<LegalProfilePermission> Permissions { get; set; } = new List<LegalProfilePermission>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public class LegalProfileFile
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên file")]
        public required string FileName { get; set; }

        [Required]
        [Display(Name = "Đường dẫn file")]
        public required string FilePath { get; set; }

        [Required]
        [Display(Name = "Loại file")]
        public required string ContentType { get; set; }

        [Display(Name = "Kích thước file (bytes)")]
        public long FileSize { get; set; }

        [Required]
        [Display(Name = "Hash SHA-256")]
        public required string FileHash { get; set; }

        public int LegalProfileId { get; set; }
        public LegalProfile LegalProfile { get; set; }
    }

    public enum LegalProfileStatus
    {
        [Display(Name = "Mới tạo")]
        Draft = 0,

        [Display(Name = "Đang xử lý")]
        Processing = 1,

        [Display(Name = "Hoàn thành")]
        Completed = 2,

        [Display(Name = "Từ chối")]
        Rejected = 3
    }
} 