// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    public sealed partial class ChaCha20Poly1305 : IDisposable
    {
        // Per https://tools.ietf.org/html/rfc7539, ChaCha20Poly1305 AEAD requires a 256-bit key and 96-bit nonce,
        // and it produces a 128-bit tag. We don't expose NonceByteSizes / TagByteSizes properties because callers
        // are expected to know this.

        private const int KeySizeInBytes = 256 / 8;
        private const int NonceSizeInBytes = 96 / 8;
        private const int TagSizeInBytes = 128 / 8;

        public ChaCha20Poly1305(ReadOnlySpan<byte> key)
        {
            ThrowIfNotSupported();

            CheckKeySize(key.Length);
            ImportKey(key);
        }

        public ChaCha20Poly1305(byte[] key)
        {
            ThrowIfNotSupported();

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            CheckKeySize(key.Length);
            ImportKey(key);
        }

        private static void CheckKeySize(int keySizeInBytes)
        {
            if (keySizeInBytes != KeySizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }
        }

        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
        {
            AeadCommon.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
            Encrypt((ReadOnlySpan<byte>)nonce, plaintext, ciphertext, tag, associatedData);
        }

        public void Encrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            CheckParameters(plaintext, ciphertext, nonce, tag);
            EncryptCore(nonce, plaintext, ciphertext, tag, associatedData);
        }

        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null)
        {
            AeadCommon.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
            Decrypt((ReadOnlySpan<byte>)nonce, ciphertext, tag, plaintext, associatedData);
        }

        public void Decrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            CheckParameters(plaintext, ciphertext, nonce, tag);
            DecryptCore(nonce, ciphertext, tag, plaintext, associatedData);
        }

        private static void CheckParameters(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag)
        {
            if (plaintext.Length != ciphertext.Length)
                throw new ArgumentException(SR.Cryptography_PlaintextCiphertextLengthMismatch);

            if (nonce.Length != NonceSizeInBytes)
                throw new ArgumentException(SR.Cryptography_InvalidNonceLength, nameof(nonce));

            if (tag.Length != TagSizeInBytes)
                throw new ArgumentException(SR.Cryptography_InvalidTagLength, nameof(tag));
        }

        private static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(ChaCha20Poly1305)));
            }
        }
    }
}
