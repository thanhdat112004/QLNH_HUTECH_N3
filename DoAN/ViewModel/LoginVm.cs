using System.ComponentModel.DataAnnotations;

namespace DoAN.ViewModels
{
    public class LoginVm
    {
        [Required]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
