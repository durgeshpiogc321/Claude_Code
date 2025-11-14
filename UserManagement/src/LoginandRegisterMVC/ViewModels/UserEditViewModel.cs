using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LoginandRegisterMVC.ViewModels;

/// <summary>
/// ViewModel for editing user information
/// </summary>
public class UserEditViewModel
{
    [Required]
    [EmailAddress]
    [MaxLength(128)]
    [Display(Name = "Email")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePictureFile { get; set; }

    [Display(Name = "Current Profile Picture")]
    public string? CurrentProfilePicture { get; set; }

    // Alias for convenience in views
    public string? ProfilePicture => CurrentProfilePicture;

    // Optional password change fields
    [DataType(DataType.Password)]
    [StringLength(500, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+=\[\]{};':""\\|,.<>/?~`-])[A-Za-z\d@$!%*?&#^()_+=\[\]{};':""\\|,.<>/?~`-]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    [Display(Name = "New Password (leave blank to keep current)")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm New Password")]
    public string? ConfirmNewPassword { get; set; }

    // Read-only display fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
