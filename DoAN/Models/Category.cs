using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

[Index("Name", Name = "UQ__Categori__737584F6FE9179A3", IsUnique = true)]
[Index("Slug", Name = "UQ__Categori__BC7B5FB676DA2949", IsUnique = true)]
public partial class Category
{
    [Key]
    [Required(ErrorMessage = "Vui lòng chọn loại món")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string Slug { get; set; } = null!;

    [InverseProperty("Category")]
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
