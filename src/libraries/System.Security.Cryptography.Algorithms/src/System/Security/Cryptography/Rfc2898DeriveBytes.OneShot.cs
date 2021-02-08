// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class Rfc2898DeriveBytes
    {
        private const int OneShotMinIterations = 1000;
        private const int OneShotMinSaltLength = 128 / 8; // 128-bits
        private const int OneShotMinExtractLength = 112 / 8; // 114-bits

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
            if (length < OneShotMinExtractLength)
                throw new ArgumentOutOfRangeException(nameof(length), SR.Format(SR.Argument_Rfc2898_MinLength, OneShotMinExtractLength));

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
            if (length < OneShotMinExtractLength)
                throw new ArgumentOutOfRangeException(nameof(length), SR.Format(SR.Argument_Rfc2898_MinLength, OneShotMinExtractLength));

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
            if (salt.Length < OneShotMinSaltLength)
                throw new ArgumentOutOfRangeException(nameof(salt), SR.Format(SR.Argument_Rfc2898_SaltTooShort, OneShotMinSaltLength));
            if (iterations < OneShotMinIterations)
                throw new ArgumentOutOfRangeException(nameof(iterations), SR.Format(SR.Argument_Rfc2898_MinIterations, OneShotMinIterations));
            if (destination.Length < OneShotMinExtractLength)
                throw new ArgumentOutOfRangeException(nameof(destination), SR.Format(SR.Argument_Rfc2898_MinLength, OneShotMinExtractLength));
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

            Pbkdf2Implementation.Fill(password, salt, iterations, hashAlgorithm.Name, destination);
        }
    }
}
