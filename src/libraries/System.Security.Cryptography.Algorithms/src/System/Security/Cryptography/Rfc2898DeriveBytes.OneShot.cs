// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class Rfc2898DeriveBytes
    {
        // Throwing UTF8 on invalid input.
        private static readonly Encoding s_throwingUtf8Encoding = new UTF8Encoding(false, true);

        public static byte[] Pbkdf2(
            byte[] password,
            byte[] salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));

            return Pbkdf2(new ReadOnlySpan<byte>(password), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
        }

        public static byte[] Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            if (outputLength < 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] result = new byte[outputLength];
            Pbkdf2(password, salt, result, iterations, hashAlgorithm);
            return result;
        }

        public static void Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
            Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
        }

        public static byte[] Pbkdf2(
            string password,
            byte[] salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));

            return Pbkdf2(password.AsSpan(), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
        }

        public static byte[] Pbkdf2(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            if (outputLength < 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] result = new byte[outputLength];
            Pbkdf2(password, salt, result, iterations, hashAlgorithm);
            return result;
        }

        public static void Pbkdf2(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
            const int MaxPasswordStackSize = 256;

            byte[]? rentedPasswordBuffer = null;
            int maxEncodedSize = s_throwingUtf8Encoding.GetMaxByteCount(password.Length);

            Span<byte> passwordBuffer = maxEncodedSize > MaxPasswordStackSize ?
                (rentedPasswordBuffer = CryptoPool.Rent(maxEncodedSize)) :
                stackalloc byte[MaxPasswordStackSize];
            int passwordBytesWritten = s_throwingUtf8Encoding.GetBytes(password, passwordBuffer);
            Span<byte> passwordBytes = passwordBuffer.Slice(0, passwordBytesWritten);

            Pbkdf2Core(passwordBytes, salt, destination, iterations, hashAlgorithm);
            CryptographicOperations.ZeroMemory(passwordBytes);

            if (rentedPasswordBuffer is not null)
            {
                CryptoPool.Return(rentedPasswordBuffer, clearSize: 0); // manually cleared above.
            }
        }

        private static void Pbkdf2Core(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
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
