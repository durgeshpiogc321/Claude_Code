using Microsoft.AspNetCore.Identity;

namespace LoginandRegisterMVC.Services;

/// <summary>
/// Secure password hashing service using ASP.NET Core Identity PasswordHasher
/// Implements PBKDF2-HMAC-SHA256 with automatic salting
/// Replaces legacy SHA1 hashing for improved security
/// </summary>
public interface ISecurePasswordHashService
{
    /// <summary>
    /// Hashes a password using PBKDF2-HMAC-SHA256
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Securely hashed password with embedded salt</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hashed password
    /// </summary>
    /// <param name="hashedPassword">Previously hashed password</param>
    /// <param name="providedPassword">Password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);

    /// <summary>
    /// Checks if a password needs rehashing (e.g., after algorithm upgrade)
    /// </summary>
    /// <param name="hashedPassword">Hashed password to check</param>
    /// <returns>True if rehashing is recommended</returns>
    bool NeedsRehash(string hashedPassword);
}

public class SecurePasswordHashService : ISecurePasswordHashService
{
    private readonly PasswordHasher<string> _hasher;
    private readonly ILogger<SecurePasswordHashService> _logger;

    public SecurePasswordHashService(ILogger<SecurePasswordHashService> logger)
    {
        _hasher = new PasswordHasher<string>();
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        var hashedPassword = _hasher.HashPassword(null!, password);

        _logger.LogDebug("Password hashed successfully using PBKDF2");

        return hashedPassword;
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
        }

        if (string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);

            if (result == PasswordVerificationResult.Success)
            {
                _logger.LogDebug("Password verification successful");
                return true;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation("Password verification successful, but rehashing recommended");
                return true;
            }

            _logger.LogWarning("Password verification failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password verification");
            return false;
        }
    }

    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            return true;
        }

        // PBKDF2 hashes from PasswordHasher start with specific markers
        // Version 3 format: "AQAAAAIAAYag..."
        // If the hash doesn't match expected format, it needs rehashing
        return !hashedPassword.StartsWith("AQAAAAI") && !hashedPassword.StartsWith("AQAAAAE");
    }
}
