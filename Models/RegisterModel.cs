using System.ComponentModel.DataAnnotations;

namespace prj1.Models
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required]
        [Display(Name = "Họ tên")]
        public required string FullName { get; set; }

        [Required]
        [Display(Name = "Số điện thoại")]
        [Phone]
        public required string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [Display(Name = "Xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public required string ConfirmPassword { get; set; }
    }
} 