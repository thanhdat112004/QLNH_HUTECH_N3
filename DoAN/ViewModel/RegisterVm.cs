using System.ComponentModel.DataAnnotations;

namespace DoAN.ViewModels
{
    public class RegisterVm
    {
        [Required, StringLength(100)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }
    }
}
