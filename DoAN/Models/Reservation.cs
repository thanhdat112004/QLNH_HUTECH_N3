using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

[Index("TableId", "ReservationDate", "ReservationTime", Name = "UX_Reservations_Slot", IsUnique = true)]
public partial class Reservation
{
    [Key]
    public int ReservationId { get; set; }

    public int? UserId { get; set; }

    [StringLength(100)]
    public string? GuestName { get; set; }

    [StringLength(100)]
    public string? GuestContact { get; set; }

    public int TableId { get; set; }

    public DateOnly ReservationDate { get; set; }

    [Precision(0)]
    public TimeOnly ReservationTime { get; set; }

    public int NumberOfGuests { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("TableId")]
    [InverseProperty("Reservations")]
    public virtual RestaurantTable Table { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reservations")]
    public virtual User? User { get; set; }
}
