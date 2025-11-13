namespace LoginandRegisterMVC.ViewModels;

/// <summary>
/// ViewModel for displaying detailed user information
/// </summary>
public class UserDetailsViewModel
{
    // User Identity
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // User Status
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    // Profile
    public string? ProfilePicture { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Computed Properties
    public string AccountStatus => IsDeleted ? "Deleted" : (IsActive ? "Active" : "Inactive");

    public string TimeSinceLastLogin
    {
        get
        {
            if (LastLoginAt == null)
                return "Never logged in";

            var timeSpan = DateTime.UtcNow - LastLoginAt.Value;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour(s) ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day(s) ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago";
        }
    }

    public string AccountAge
    {
        get
        {
            var timeSpan = DateTime.UtcNow - CreatedAt;

            if (timeSpan.TotalDays < 1)
                return "Less than a day";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day(s)";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month(s)";

            return $"{(int)(timeSpan.TotalDays / 365)} year(s)";
        }
    }

    // Password Migration Status
    public bool PasswordMigrated { get; set; }
    public string PasswordSecurityStatus => PasswordMigrated ? "Secure (PBKDF2)" : "Legacy (SHA1 - needs migration)";
}
