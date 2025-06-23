using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

public partial class MenuItem
{
    [Key]
    public int MenuItemId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsFeatured { get; set; }

    public int FeaturedOrder { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("MenuItems")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("MenuItem")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [InverseProperty("MenuItem")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
