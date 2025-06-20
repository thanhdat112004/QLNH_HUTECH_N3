using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

public partial class EmailVerification
{
    [Key]
    public int VerificationId { get; set; }

    public int UserId { get; set; }

    public Guid Token { get; set; }

    [StringLength(20)]
    public string Type { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmailVerifications")]
    public virtual User User { get; set; } = null!;
}
