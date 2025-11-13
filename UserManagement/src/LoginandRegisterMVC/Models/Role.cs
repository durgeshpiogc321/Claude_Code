using System.ComponentModel.DataAnnotations;

namespace LoginandRegisterMVC.Models;

/// <summary>
/// Represents a user role in the system
/// </summary>
public class Role
{
    [Key]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
