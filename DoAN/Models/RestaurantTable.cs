using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

[Index("TableNumber", Name = "UQ__Restaura__E8E0DB523D34B8B9", IsUnique = true)]
public partial class RestaurantTable
{
    [Key]
    public int TableId { get; set; }

    [StringLength(10)]
    public string TableNumber { get; set; } = null!;

    public int Seats { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [InverseProperty("Table")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Table")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
