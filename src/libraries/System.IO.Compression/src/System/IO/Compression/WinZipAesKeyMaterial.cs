// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace System.IO.Compression
{
    /// <summary>
    /// Represents the parsed components of WinZip AES key material.
    /// The key material layout is: [salt][encryption key][HMAC key][password verifier (2 bytes)].
    /// </summary>
    internal readonly struct WinZipAesKeyMaterial
    {
        public byte[] Salt { get; }
        public byte[] EncryptionKey { get; }
        public byte[] HmacKey { get; }
        public byte[] PasswordVerifier { get; }
        public int KeySizeBits { get; }
        public int SaltSize { get; }

        private WinZipAesKeyMaterial(byte[] salt, byte[] encryptionKey, byte[] hmacKey, byte[] passwordVerifier, int keySizeBits)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
            }
            Salt = salt;
            EncryptionKey = encryptionKey;
            HmacKey = hmacKey;
            PasswordVerifier = passwordVerifier;
            KeySizeBits = keySizeBits;
            SaltSize = GetSaltSize(keySizeBits);
        }

        /// <summary>
        /// Derives key material from a password and optional salt using PBKDF2-SHA1.
        /// </summary>
        internal static unsafe WinZipAesKeyMaterial Create(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits)
        {
            if (OperatingSystem.IsBrowser())
            {
                throw new PlatformNotSupportedException(SR.WinZipEncryptionNotSupportedOnBrowser);
            }
            int saltSize = GetSaltSize(keySizeBits);
            int keySizeBytes = keySizeBits / 8;
            int totalKeySize = checked(keySizeBytes + keySizeBytes + 2);

            byte[] saltBytes;
            if (salt is null)
            {
                saltBytes = new byte[saltSize];
                RandomNumberGenerator.Fill(saltBytes);
            }
            else
            {
                Debug.Assert(salt.Length == saltSize, $"Salt must be {saltSize} bytes for AES-{keySizeBits}.");
                saltBytes = salt;
            }

            int maxPasswordByteCount = Encoding.UTF8.GetMaxByteCount(password.Length);
            byte[] rentedPasswordBytes = ArrayPool<byte>.Shared.Rent(maxPasswordByteCount);
            Debug.Assert(totalKeySize <= 66, "totalKeySize should be at most 66 bytes (AES-256: 32 + 32 + 2)");
            Span<byte> derivedKey = stackalloc byte[totalKeySize];

            try
            {
                int actualByteCount = Encoding.UTF8.GetBytes(password, rentedPasswordBytes);
                Span<byte> passwordSpan = rentedPasswordBytes.AsSpan(0, actualByteCount);

                Rfc2898DeriveBytes.Pbkdf2(
                    passwordSpan,
                    saltBytes,
                    derivedKey,
                    1000, // iteration count specified by the WinZip AE-1/AE-2 specification
                    HashAlgorithmName.SHA1);

                // Slice the derived key directly into its components instead of
                // round-tripping through a combined array and Parse.
                byte[] encryptionKey = derivedKey.Slice(0, keySizeBytes).ToArray();
                byte[] hmacKey = derivedKey.Slice(keySizeBytes, keySizeBytes).ToArray();
                byte[] passwordVerifier = derivedKey.Slice(keySizeBytes + keySizeBytes, 2).ToArray();

                return new WinZipAesKeyMaterial(saltBytes, encryptionKey, hmacKey, passwordVerifier, keySizeBits);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedPasswordBytes);
                CryptographicOperations.ZeroMemory(derivedKey);
                ArrayPool<byte>.Shared.Return(rentedPasswordBytes);
            }
        }

        internal static int GetSaltSize(int keySizeBits) => keySizeBits / 16;
    }
}
