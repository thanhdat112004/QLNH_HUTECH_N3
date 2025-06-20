using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

public partial class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public int? UserId { get; set; }

    [StringLength(100)]
    public string CustomerName { get; set; } = null!;

    [StringLength(200)]
    public string CustomerContact { get; set; } = null!;

    [StringLength(20)]
    public string OrderType { get; set; } = null!;

    public int? TableId { get; set; }

    public DateTime InvoiceDate { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = null!;

    [InverseProperty("Invoice")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [ForeignKey("OrderId")]
    [InverseProperty("Invoices")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Invoices")]
    public virtual User? User { get; set; }
}
