namespace LoginandRegisterMVC.ViewModels;

/// <summary>
/// ViewModel for displaying user in a list
/// </summary>
public class UserListViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// ViewModel for paginated user list with search and filter options
/// </summary>
public class UserListPageViewModel
{
    public IEnumerable<Models.User> Users { get; set; } = new List<Models.User>();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalUsers { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalUsers / PageSize);

    // Search and Filter
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
    public bool? ActiveFilter { get; set; }

    // Sorting
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";

    // Statistics
    public int ActiveUserCount { get; set; }
    public int TotalUserCount { get; set; }
}
