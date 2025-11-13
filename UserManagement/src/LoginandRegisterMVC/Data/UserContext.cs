using Microsoft.EntityFrameworkCore;
using LoginandRegisterMVC.Models;

namespace LoginandRegisterMVC.Data;

public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Role).IsRequired();

            // Configure DateTime columns to use DATETIME2 for precision
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime2");

            entity.Property(e => e.LastLoginAt)
                .HasColumnType("datetime2");

            // Performance Indexes
            // Composite index for filtering active/non-deleted users (most common query)
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted })
                .HasDatabaseName("IX_Users_IsActive_IsDeleted");

            // Index for sorting by creation date (user lists)
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Users_CreatedAt");

            // Index for sorting by last login (activity reports)
            entity.HasIndex(e => e.LastLoginAt)
                .HasDatabaseName("IX_Users_LastLoginAt");

            // Composite index for email lookups excluding deleted users
            entity.HasIndex(e => new { e.UserId, e.IsDeleted })
                .HasDatabaseName("IX_Users_UserId_IsDeleted");

            // Global query filter to exclude soft-deleted users by default
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
