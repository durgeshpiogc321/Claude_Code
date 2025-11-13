using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginandRegisterMVC.Models;

public class User
{
    [Key]
    [Required]
    [EmailAddress]
    [MaxLength(128)]
    [DataType(DataType.EmailAddress)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+=\[\]{};':""\\|,.<>/?~`-])[A-Za-z\d@$!%*?&#^()_+=\[\]{};':""\\|,.<>/?~`-]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public string Password { get; set; } = string.Empty;

    [NotMapped]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Confirm Password required")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // SECURITY FIX: Removed [Required] attribute - Role is now server-assigned, not user-submitted
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    // Password Migration Support
    /// <summary>
    /// New secure password hash (PBKDF2) - replaces SHA1 password
    /// </summary>
    [StringLength(500)]
    public string? PasswordV2 { get; set; }

    /// <summary>
    /// Indicates if user has migrated to secure password hashing
    /// </summary>
    public bool PasswordMigrated { get; set; } = false;

    // User Management Enhancement Fields
    /// <summary>
    /// Indicates whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft delete flag - indicates if user is deleted (for audit trail)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the user account was soft-deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Path or URL to user's profile picture
    /// </summary>
    [StringLength(500)]
    public string? ProfilePicture { get; set; }

    /// <summary>
    /// Timestamp of user's last successful login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
