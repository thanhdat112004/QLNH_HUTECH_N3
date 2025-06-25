using System.ComponentModel.DataAnnotations;

namespace DoAN.Models.ViewModels
{
    public class InvoiceEditViewModel
    {
        public int InvoiceId { get; set; }

        [Display(Name = "Khách hàng")]
        public string? FullName { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string? Username { get; set; }

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Range(0, 100000000)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = null!;
    }
}
