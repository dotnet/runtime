// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class Rfc2898DeriveBytes
    {
        // Throwing UTF8 on invalid input.
        private static readonly UTF8Encoding s_throwingUtf8Encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// Creates a PBKDF2 derived key from password bytes.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="outputLength">The size of key to derive.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="password" /> or <paramref name="salt" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="outputLength" /> is not zero or a positive value.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="iterations" /> is not a positive value.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        public static byte[] Pbkdf2(
            byte[] password,
            byte[] salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(salt);

            return Pbkdf2(new ReadOnlySpan<byte>(password), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
        }

        /// <summary>
        /// Creates a PBKDF2 derived key from password bytes.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="outputLength">The size of key to derive.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="outputLength" /> is not zero or a positive value.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="iterations" /> is not a positive value.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        public static byte[] Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            ValidateHashAlgorithm(hashAlgorithm);

            byte[] result = new byte[outputLength];
            Pbkdf2Core(password, salt, result, iterations, hashAlgorithm);
            return result;
        }

        /// <summary>
        /// Fills a buffer with a PBKDF2 derived key.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="destination">The buffer to fill with a derived key.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="iterations" /> is not a positive value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        public static void Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            ValidateHashAlgorithm(hashAlgorithm);

            Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
        }

        /// <summary>
        /// Creates a PBKDF2 derived key from a password.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="outputLength">The size of key to derive.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="password" /> or <paramref name="salt" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="outputLength" /> is not zero or a positive value.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="iterations" /> is not a positive value.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        /// <paramref name="password" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        /// The <paramref name="password" /> will be converted to bytes using the UTF-8 encoding. For
        /// other encodings, convert the password string to bytes using the appropriate <see cref="System.Text.Encoding" />
        /// and use <see cref="Pbkdf2(byte[], byte[], int, HashAlgorithmName, int)" />.
        /// </remarks>
        public static byte[] Pbkdf2(
            string password,
            byte[] salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(salt);

            return Pbkdf2(password.AsSpan(), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
        }

        /// <summary>
        /// Creates a PBKDF2 derived key from a password.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="outputLength">The size of key to derive.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="outputLength" /> is not zero or a positive value.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="iterations" /> is not a positive value.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        /// <paramref name="password" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        /// The <paramref name="password" /> will be converted to bytes using the UTF-8 encoding. For
        /// other encodings, convert the password string to bytes using the appropriate <see cref="System.Text.Encoding" />
        /// and use <see cref="Pbkdf2(ReadOnlySpan{byte}, ReadOnlySpan{byte}, int, HashAlgorithmName, int)" />.
        /// </remarks>
        public static byte[] Pbkdf2(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithm,
            int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            ValidateHashAlgorithm(hashAlgorithm);

            byte[] result = new byte[outputLength];
            Pbkdf2Core(password, salt, result, iterations, hashAlgorithm);
            return result;
        }

        /// <summary>
        /// Fills a buffer with a PBKDF2 derived key.
        /// </summary>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to derive the key.</param>
        /// <param name="destination">The buffer to fill with a derived key.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="iterations" /> is not a positive value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm" /> has a <see cref="HashAlgorithmName.Name" />
        ///   that is empty or <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm" /> is an unsupported hash algorithm. Supported algorithms
        ///   are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
        ///   <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
        /// </exception>
        /// <exception cref="EncoderFallbackException">
        /// <paramref name="password" /> contains text that cannot be converted to UTF-8.
        /// </exception>
        /// <remarks>
        /// The <paramref name="password" /> will be converted to bytes using the UTF-8 encoding. For
        /// other encodings, convert the password string to bytes using the appropriate <see cref="System.Text.Encoding" />
        /// and use <see cref="Pbkdf2(ReadOnlySpan{byte}, ReadOnlySpan{byte}, Span{byte}, int, HashAlgorithmName)" />.
        /// </remarks>
        public static void Pbkdf2(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            ValidateHashAlgorithm(hashAlgorithm);

            Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
        }

        private static void Pbkdf2Core(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> salt,
            Span<byte> destination,
            int iterations,
            HashAlgorithmName hashAlgorithm)
        {
            Debug.Assert(hashAlgorithm.Name is not null);
            Debug.Assert(iterations > 0);

            if (destination.IsEmpty)
            {
                return;
            }

            const int MaxPasswordStackSize = 256;

            byte[]? rentedPasswordBuffer = null;
            int maxEncodedSize = s_throwingUtf8Encoding.GetMaxByteCount(password.Length);

            Span<byte> passwordBuffer = maxEncodedSize > MaxPasswordStackSize ?
                (rentedPasswordBuffer = CryptoPool.Rent(maxEncodedSize)) :
                stackalloc byte[MaxPasswordStackSize];
            int passwordBytesWritten = s_throwingUtf8Encoding.GetBytes(password, passwordBuffer);
            Span<byte> passwordBytes = passwordBuffer.Slice(0, passwordBytesWritten);

            try
            {
                Pbkdf2Implementation.Fill(passwordBytes, salt, iterations, hashAlgorithm, destination);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }

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
            Debug.Assert(hashAlgorithm.Name is not null);
            Debug.Assert(iterations > 0);

            if (destination.IsEmpty)
            {
                return;
            }

            Pbkdf2Implementation.Fill(password, salt, iterations, hashAlgorithm, destination);
        }

        private static void ValidateHashAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));

            // MD5 intentionally left out.
            if (hashAlgorithmName == HashAlgorithmName.SHA1.Name ||
                hashAlgorithmName == HashAlgorithmName.SHA256.Name ||
                hashAlgorithmName == HashAlgorithmName.SHA384.Name ||
                hashAlgorithmName == HashAlgorithmName.SHA512.Name)
            {
                return;
            }

            if (hashAlgorithmName == HashAlgorithmName.SHA3_256.Name ||
                hashAlgorithmName == HashAlgorithmName.SHA3_384.Name ||
                hashAlgorithmName == HashAlgorithmName.SHA3_512.Name)
            {
                // All current platforms support HMAC-SHA3-256, 384, and 512 together, so we can simplify the check
                // to just checking HMAC-SHA3-256 for the availability of 384 and 512, too.
                if (HMACSHA3_256.IsSupported)
                {
                    return;
                }

                throw new PlatformNotSupportedException();
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName));
        }
    }
}
