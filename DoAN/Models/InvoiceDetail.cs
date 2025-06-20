using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

public partial class InvoiceDetail
{
    [Key]
    public int InvoiceDetailId { get; set; }

    public int InvoiceId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(21, 2)")]
    public decimal? LineTotal { get; set; }

    [ForeignKey("InvoiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Invoice Invoice { get; set; } = null!;

    [ForeignKey("MenuItemId")]
    [InverseProperty("InvoiceDetails")]
    public virtual MenuItem MenuItem { get; set; } = null!;
}
