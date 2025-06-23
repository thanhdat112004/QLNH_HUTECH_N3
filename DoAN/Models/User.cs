using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

[Index("Username", Name = "UQ__Users__536C85E42EDF8F8F", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D105344979C149", IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(256)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(256)]
    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<EmailVerification> EmailVerifications { get; set; } = new List<EmailVerification>();

    [InverseProperty("User")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("User")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [InverseProperty("User")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
