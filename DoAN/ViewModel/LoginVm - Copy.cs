using System.ComponentModel.DataAnnotations;

namespace DoAN.Models.ViewModels
{
    public class InvoiceEditViewModel
    {
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string CustomerContact { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Range(0, 100000000)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = null!;
    }
}
