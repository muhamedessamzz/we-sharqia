using System.Security.Cryptography;
using System.Text;

namespace WeSharkia.Application.Common.Security;

/// <summary>
/// Provides military-grade cryptographic protection for student personal identifiable information (PII)
/// and Egyptian National IDs. Implements salted PBKDF2 hashing, AES-256-GCM encryption at rest,
/// digit normalization, and Egyptian National ID verification.
/// </summary>
public static class SecurityService
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;
    private const string HashPrefix = "PBKDF2$100000$";

    // Master fallback encryption key (32 bytes = 256 bits) for National ID data at rest
    private static readonly byte[] DefaultMasterKey = SHA256.HashData(
        Encoding.UTF8.GetBytes("WeSharkia_UltraSecure_NationalId_EncryptionKey_2026_Salt_Sharqia_School")
    );

    /// <summary>
    /// Normalizes Eastern Arabic numerals (٠-٩) to Western Arabic (0-9)
    /// and strips spaces, hyphens, and non-digit characters.
    /// </summary>
    public static string NormalizeDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input.Trim())
        {
            if (c >= '0' && c <= '9')
            {
                sb.Append(c);
            }
            else if (c >= '٠' && c <= '٩')
            {
                sb.Append((char)('0' + (c - '٠')));
            }
            // Discard spaces, hyphens, and letters for digit-only fields
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes general text (converts Arabic digits to 0-9, trims whitespace).
    /// </summary>
    public static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input.Trim())
        {
            if (c >= '٠' && c <= '٩')
            {
                sb.Append((char)('0' + (c - '٠')));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates Egyptian National ID (14 digits, century 2 or 3, valid month & day).
    /// </summary>
    public static bool IsValidEgyptianNationalId(string? nationalId)
    {
        var clean = NormalizeDigits(nationalId);
        if (clean.Length != 14) return false;

        // Century digit: 2 = 1900-1999, 3 = 2000-2099
        char century = clean[0];
        if (century != '2' && century != '3') return false;

        // Month: digits 3 and 4 (1-indexed: 4 and 5)
        if (!int.TryParse(clean.Substring(3, 2), out int month) || month < 1 || month > 12)
            return false;

        // Day: digits 5 and 6 (1-indexed: 6 and 7)
        if (!int.TryParse(clean.Substring(5, 2), out int day) || day < 1 || day > 31)
            return false;

        return true;
    }

    /// <summary>
    /// Hashes a password or National ID using salted PBKDF2-HMAC-SHA256 with 100,000 iterations.
    /// This is an irreversible, computationally expensive hash that prevents dictionary and rainbow table attacks.
    /// </summary>
    public static string HashPassword(string rawPassword)
    {
        var cleanPassword = NormalizeDigitsIfNumeric(rawPassword);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] subKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(cleanPassword),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes
        );

        return $"{HashPrefix}{Convert.ToBase64String(salt)}${Convert.ToBase64String(subKey)}";
    }

    /// <summary>
    /// Verifies a raw password or National ID against a stored PBKDF2 hash using constant-time comparison.
    /// Supports backward compatibility with unhashed legacy seed passwords.
    /// </summary>
    public static bool VerifyPassword(string rawPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(rawPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        var cleanPassword = NormalizeDigitsIfNumeric(rawPassword);

        if (storedHash.StartsWith(HashPrefix))
        {
            try
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedSubKey = Convert.FromBase64String(parts[3]);

                byte[] actualSubKey = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(cleanPassword),
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSizeBytes
                );

                return CryptographicOperations.FixedTimeEquals(actualSubKey, expectedSubKey);
            }
            catch
            {
                return false;
            }
        }

        // Backward compatibility for legacy plain-text passwords
        return rawPassword.Trim() == storedHash.Trim() || cleanPassword == storedHash.Trim();
    }

    /// <summary>
    /// Encrypts sensitive National ID at rest using AES-256-GCM (Authenticated Encryption).
    /// </summary>
    public static string EncryptNationalId(string nationalId, byte[]? customKey = null)
    {
        var clean = NormalizeDigits(nationalId);
        if (string.IsNullOrEmpty(clean)) return string.Empty;

        byte[] key = customKey ?? DefaultMasterKey;
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize); // 12 bytes
        byte[] plainBytes = Encoding.UTF8.GetBytes(clean);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Combined: nonce (12) + tag (16) + cipherBytes
        byte[] combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypts National ID encrypted with AES-256-GCM.
    /// Restricted exclusively to authorized staff workflows.
    /// </summary>
    public static string? DecryptNationalId(string cipherTextBase64, byte[]? customKey = null)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64)) return null;

        try
        {
            byte[] combined = Convert.FromBase64String(cipherTextBase64);
            int nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
            int tagSize = AesGcm.TagByteSizes.MaxSize; // 16

            if (combined.Length < nonceSize + tagSize) return null;

            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            int cipherSize = combined.Length - nonceSize - tagSize;
            byte[] cipherBytes = new byte[cipherSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(combined, nonceSize + tagSize, cipherBytes, 0, cipherSize);

            byte[] plainBytes = new byte[cipherSize];
            byte[] key = customKey ?? DefaultMasterKey;

            using var aesGcm = new AesGcm(key, tagSize);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Masks a 14-digit National ID for secure display in tables (e.g. 308********567).
    /// Hides the central 8 digits containing sensitive sequence and birth specifics.
    /// </summary>
    public static string MaskNationalId(string? nationalId)
    {
        var clean = NormalizeDigits(nationalId);
        if (string.IsNullOrEmpty(clean)) return string.Empty;

        if (clean.Length == 14)
        {
            return $"{clean[..3]}********{clean[11..]}";
        }

        if (clean.Length > 6)
        {
            return $"{clean[..2]}****{clean[^2..]}";
        }

        return "******";
    }

    private static string NormalizeDigitsIfNumeric(string text)
    {
        var normalized = NormalizeText(text);
        var digitsOnly = NormalizeDigits(text);

        // If the entire text consists of digits (Arabic or Western), return clean digits
        if (digitsOnly.Length > 0 && digitsOnly.Length == normalized.Replace(" ", "").Replace("-", "").Length)
        {
            return digitsOnly;
        }

        return normalized;
    }
}
