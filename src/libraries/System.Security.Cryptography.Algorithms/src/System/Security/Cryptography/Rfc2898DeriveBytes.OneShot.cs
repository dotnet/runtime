// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class Rfc2898DeriveBytes
    {
        public static byte[] Pbkdf2DeriveBytes(
            byte[] password,
            byte[] salt,
            int iterations,
            int length,
            HashAlgorithmName hashAlgorithm)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));

            return Pbkdf2DeriveBytes(new ReadOnlySpan<byte>(password), new ReadOnlySpan<byte>(salt), iterations, length, hashAlgorithm);
        }

        public static byte[] Pbkdf2DeriveBytes(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            int length,
            HashAlgorithmName hashAlgorithm)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] result = new byte[length];
            Pbkdf2DeriveBytes(password, salt, iterations, hashAlgorithm, result);
            return result;
        }

        public static void Pbkdf2DeriveBytes(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            Span<byte> destination)
        {
            Pbkdf2DeriveBytesCore(password, salt, iterations, hashAlgorithm, destination);
        }

        public static byte[] Pbkdf2DeriveBytes(
            string password,
            byte[] salt,
            int iterations,
            int length,
            HashAlgorithmName hashAlgorithm)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));

            return Pbkdf2DeriveBytes(password.AsSpan(), new ReadOnlySpan<byte>(salt), iterations, length, hashAlgorithm);
        }

        public static byte[] Pbkdf2DeriveBytes(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            int length,
            HashAlgorithmName hashAlgorithm)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] result = new byte[length];
            Pbkdf2DeriveBytes(password, salt, iterations, hashAlgorithm, result);
            return result;
        }

        public static void Pbkdf2DeriveBytes(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            Span<byte> destination)
        {
            byte[] passwordBytes = CryptoPool.Rent(Encoding.UTF8.GetMaxByteCount(password.Length));
            int passwordBytesWritten = Encoding.UTF8.GetBytes(password, passwordBytes);

            Pbkdf2DeriveBytesCore(passwordBytes, salt, iterations, hashAlgorithm, destination);
            CryptoPool.Return(passwordBytes, clearSize: passwordBytesWritten);
        }

        private static void Pbkdf2DeriveBytesCore(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            Span<byte> destination)
        {
            if (salt.Length < MinimumSaltSize)
                throw new ArgumentOutOfRangeException(nameof(salt), SR.Cryptography_PasswordDerivedBytes_FewBytesSalt);
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(iterations), SR.ArgumentOutOfRange_NeedPosNum);
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            string hashAlgorithmName = hashAlgorithm.Name;

            // MD5 intentionally left out.
            if (hashAlgorithmName != HashAlgorithmName.SHA1.Name &&
                hashAlgorithmName != HashAlgorithmName.SHA256.Name &&
                hashAlgorithmName != HashAlgorithmName.SHA384.Name &&
                hashAlgorithmName != HashAlgorithmName.SHA512.Name)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName));
            }

            if (destination.IsEmpty)
            {
                return;
            }

            Pbkdf2Implementation.Fill(password, salt, iterations, hashAlgorithm, destination);
        }
    }
}
