using System;
using System.ComponentModel.DataAnnotations;

namespace prj1.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int LegalProfileId { get; set; }
        public LegalProfile LegalProfile { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public required string UserName { get; set; }

        [Required]
        public required string Action { get; set; }

        [Required]
        public required string ChangedFields { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
} 