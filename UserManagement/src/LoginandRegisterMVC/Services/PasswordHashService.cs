using System.Security.Cryptography;
using System.Text;

namespace LoginandRegisterMVC.Services;

/// <summary>
/// DEPRECATED: Legacy password hashing service using SHA1
/// WARNING: SHA1 is cryptographically broken. Use ISecurePasswordHashService instead.
/// This service is kept temporarily for backward compatibility during migration.
/// </summary>
[Obsolete("SHA1 hashing is insecure. Use ISecurePasswordHashService for new implementations.")]
public interface IPasswordHashService
{
    string HashPassword(string password);
}

/// <summary>
/// DEPRECATED: Legacy SHA1 password hasher
/// Security Issue: SHA1 is vulnerable to collision attacks and rainbow tables
/// Migration Strategy: New passwords use PBKDF2, old passwords migrate on successful login
/// </summary>
[Obsolete("SHA1 hashing is insecure. Use SecurePasswordHashService for new implementations.")]
public class PasswordHashService : IPasswordHashService
{
    private readonly ILogger<PasswordHashService> _logger;

    public PasswordHashService(ILogger<PasswordHashService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Legacy SHA1 password hashing (INSECURE - for backward compatibility only)
    /// </summary>
    public string HashPassword(string password)
    {
        _logger.LogWarning("Legacy SHA1 password hashing used. This is insecure and should be migrated.");

        var pwdarray = Encoding.ASCII.GetBytes(password);
#pragma warning disable SYSLIB0021 // Type or member is obsolete (SHA1 intentionally used for legacy support)
        var sha1 = SHA1.Create();
#pragma warning restore SYSLIB0021
        var hash = sha1.ComputeHash(pwdarray);
        var hashpwd = new StringBuilder(hash.Length);
        foreach (byte b in hash)
        {
            hashpwd.Append(b.ToString());
        }
        return hashpwd.ToString();
    }
}
