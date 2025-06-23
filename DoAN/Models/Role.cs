using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Models;

[Index("Name", Name = "UQ__Roles__737584F673016B99", IsUnique = true)]
public partial class Role
{
    [Key]
    public int RoleId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [InverseProperty("Role")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
