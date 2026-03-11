// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace System.IO.Compression
{
    /// <summary>
    /// Represents the parsed components of WinZip AES key material.
    /// The key material layout is: [salt][encryption key][HMAC key][password verifier (2 bytes)].
    /// </summary>
    [UnsupportedOSPlatform("browser")]
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
            Salt = salt;
            EncryptionKey = encryptionKey;
            HmacKey = hmacKey;
            PasswordVerifier = passwordVerifier;
            KeySizeBits = keySizeBits;
            SaltSize = GetSaltSize(keySizeBits);
        }

        /// <summary>
        /// Parses raw key material bytes into their individual components.
        /// Validates that the input length matches the expected layout for the given key size.
        /// </summary>
        internal static WinZipAesKeyMaterial Parse(byte[] keyMaterial, int keySizeBits)
        {
            int saltSize = GetSaltSize(keySizeBits);
            int keySizeBytes = keySizeBits / 8;
            int expectedSize = checked(saltSize + keySizeBytes + keySizeBytes + 2);

            if (keyMaterial.Length != expectedSize)
            {
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            }

            int offset = 0;

            byte[] salt = new byte[saltSize];
            Array.Copy(keyMaterial, offset, salt, 0, saltSize);
            offset += saltSize;

            byte[] encryptionKey = new byte[keySizeBytes];
            Array.Copy(keyMaterial, offset, encryptionKey, 0, keySizeBytes);
            offset += keySizeBytes;

            byte[] hmacKey = new byte[keySizeBytes];
            Array.Copy(keyMaterial, offset, hmacKey, 0, keySizeBytes);
            offset += keySizeBytes;

            byte[] passwordVerifier = new byte[2];
            Array.Copy(keyMaterial, offset, passwordVerifier, 0, 2);

            return new WinZipAesKeyMaterial(salt, encryptionKey, hmacKey, passwordVerifier, keySizeBits);
        }

        /// <summary>
        /// Derives key material from a password and optional salt using PBKDF2-SHA1.
        /// </summary>
        internal static WinZipAesKeyMaterial Create(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits)
        {
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
                if (salt.Length != saltSize)
                {
                    throw new ArgumentException($"Salt must be {saltSize} bytes for AES-{keySizeBits}.", nameof(salt));
                }
                saltBytes = salt;
            }

            int maxPasswordByteCount = Encoding.UTF8.GetMaxByteCount(password.Length);
            byte[] rentedPasswordBytes = System.Buffers.ArrayPool<byte>.Shared.Rent(maxPasswordByteCount);
            // totalKeySize is at most 66 bytes (AES-256: 32 + 32 + 2), safe for stackalloc
            Span<byte> derivedKey = stackalloc byte[totalKeySize];

            try
            {
                int actualByteCount = Encoding.UTF8.GetBytes(password, rentedPasswordBytes);
                Span<byte> passwordSpan = rentedPasswordBytes.AsSpan(0, actualByteCount);

                Rfc2898DeriveBytes.Pbkdf2(
                    passwordSpan,
                    saltBytes,
                    derivedKey,
                    1000,
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
                System.Buffers.ArrayPool<byte>.Shared.Return(rentedPasswordBytes);
            }
        }

        internal static int GetSaltSize(int keySizeBits) => keySizeBits / 16;
    }
}
