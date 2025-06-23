using System;
using System.Collections.Generic;
using DoAN.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<RestaurantTable> RestaurantTables { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B3B4DC4DB");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__EmailVer__306D4907CEFA4FE2");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Token).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerifications_User");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB58341EF7F");

            entity.Property(e => e.InvoiceDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Order).WithMany(p => p.Invoices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Order");

            entity.HasOne(d => d.User).WithMany(p => p.Invoices).HasConstraintName("FK_Invoices_User");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.InvoiceDetailId).HasName("PK__InvoiceD__1F157811325195F7");

            entity.Property(e => e.LineTotal).HasComputedColumnSql("([Quantity]*[UnitPrice])", true);

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceDetails_Invoice");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.InvoiceDetails)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceDetails_MenuItem");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(e => e.MenuItemId).HasName("PK__MenuItem__8943F72227E5BFEC");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MenuItems_Categories");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF8791FB05");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderType).HasDefaultValue("DineIn");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Table).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Table");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_User");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED0681CEF7F9CD");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_MenuItem");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Order");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38F66032E0");

            entity.Property(e => e.PaidAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Order");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PK__Reservat__B7EE5F24F3CA093D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NumberOfGuests).HasDefaultValue(1);
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Table).WithMany(p => p.Reservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Table");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations).HasConstraintName("FK_Reservations_User");
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.HasKey(e => e.TableId).HasName("PK__Restaura__7D5F01EE222527C5");

            entity.Property(e => e.Status).HasDefaultValue("Available");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A1357B7B1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CCEB777BA");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Role");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
