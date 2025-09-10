using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace BadeHava.Utils;

public static class PasswordHasher
{
    public static (string hashPassword, byte[] salt) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 128 / 8
        ));

        return (hash, salt);
    }

    public static bool Validate(string password, string storedHash, byte[] storedSalt)
    {
        string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        password: password,
        salt: storedSalt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 128 / 8));

        return hash == storedHash;
    }
}