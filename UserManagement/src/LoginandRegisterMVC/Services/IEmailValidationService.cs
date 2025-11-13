namespace LoginandRegisterMVC.Services;

/// <summary>
/// Service interface for email validation and verification.
/// </summary>
public interface IEmailValidationService
{
    /// <summary>
    /// Validates email format using regex pattern.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email format is valid, false otherwise</returns>
    bool IsValidEmailFormat(string email);

    /// <summary>
    /// Checks if email domain is from a disposable email provider.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if disposable email, false otherwise</returns>
    bool IsDisposableEmail(string email);

    /// <summary>
    /// Validates email with comprehensive checks (format, disposable domain, etc.).
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>Tuple containing validation result and error message (if invalid)</returns>
    (bool IsValid, string? ErrorMessage) ValidateEmail(string email);

    /// <summary>
    /// Normalizes email address (lowercase, trim whitespace).
    /// </summary>
    /// <param name="email">Email address to normalize</param>
    /// <returns>Normalized email address</returns>
    string NormalizeEmail(string email);
}
