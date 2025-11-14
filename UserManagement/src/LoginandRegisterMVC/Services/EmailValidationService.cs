using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace LoginandRegisterMVC.Services;

/// <summary>
/// Service for email validation and verification.
/// </summary>
public class EmailValidationService(ILogger<EmailValidationService> logger) : IEmailValidationService
{
    private readonly ILogger<EmailValidationService> _logger = logger;

    // RFC 5322 compliant email regex pattern
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Common disposable email domains (subset - can be expanded)
    private static readonly HashSet<string> DisposableEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "tempmail.com",
        "guerrillamail.com",
        "10minutemail.com",
        "mailinator.com",
        "throwaway.email",
        "temp-mail.org",
        "fakeinbox.com",
        "yopmail.com",
        "trashmail.com",
        "maildrop.cc",
        "getnada.com",
        "temp-mail.io",
        "dispostable.com",
        "mohmal.com",
        "sharklasers.com",
        "guerrillamailblock.com",
        "spam4.me",
        "grr.la",
        "mintemail.com",
        "emailondeck.com"
    };

    /// <summary>
    /// Validates email format using RFC 5322 compliant regex pattern.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email format is valid, false otherwise</returns>
    public bool IsValidEmailFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogDebug("Email validation failed: null or empty email");
            return false;
        }

        // Additional length validation
        if (email.Length > 254) // RFC 5321 maximum email length
        {
            _logger.LogDebug("Email validation failed: email too long (>{Length})", email.Length);
            return false;
        }

        var isValid = EmailRegex.IsMatch(email);

        if (!isValid)
        {
            _logger.LogDebug("Email validation failed: invalid format for {Email}", email);
        }

        return isValid;
    }

    /// <summary>
    /// Checks if email domain is from a disposable email provider.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if disposable email, false otherwise</returns>
    public bool IsDisposableEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(domain))
            {
                return false;
            }

            var isDisposable = DisposableEmailDomains.Contains(domain);

            if (isDisposable)
            {
                _logger.LogWarning("Disposable email domain detected: {Domain}", domain);
            }

            return isDisposable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disposable email for {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Validates email with comprehensive checks (format, disposable domain, etc.).
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>Tuple containing validation result and error message (if invalid)</returns>
    public (bool IsValid, string? ErrorMessage) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogDebug("Email validation failed: null or empty");
            return (false, "Email address is required.");
        }

        // Normalize and validate format
        var normalizedEmail = NormalizeEmail(email);

        if (!IsValidEmailFormat(normalizedEmail))
        {
            _logger.LogDebug("Email validation failed: invalid format for {Email}", normalizedEmail);
            return (false, "Email address format is invalid.");
        }

        // Check for disposable email domains
        if (IsDisposableEmail(normalizedEmail))
        {
            _logger.LogWarning("Email validation failed: disposable email detected for {Email}", normalizedEmail);
            return (false, "Disposable email addresses are not allowed. Please use a permanent email address.");
        }

        // Check for common invalid patterns
        if (normalizedEmail.Contains("..") || normalizedEmail.StartsWith('.') || normalizedEmail.EndsWith('.'))
        {
            _logger.LogDebug("Email validation failed: invalid character patterns in {Email}", normalizedEmail);
            return (false, "Email address contains invalid character patterns.");
        }

        _logger.LogDebug("Email validation successful for {Email}", normalizedEmail);
        return (true, null);
    }

    /// <summary>
    /// Normalizes email address (lowercase, trim whitespace).
    /// </summary>
    /// <param name="email">Email address to normalize</param>
    /// <returns>Normalized email address</returns>
    public string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        try
        {
            // Trim whitespace and convert to lowercase
            var normalized = email.Trim().ToLowerInvariant();

            _logger.LogTrace("Email normalized: {Original} -> {Normalized}", email, normalized);

            return normalized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing email: {Email}", email);
            return email.Trim();
        }
    }
}
