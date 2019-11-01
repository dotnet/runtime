// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class AesCcm : IDisposable
    {
        public static KeySizes NonceByteSizes { get; } = new KeySizes(7, 13, 1);
        public static KeySizes TagByteSizes { get; } = new KeySizes(4, 16, 2);

        public AesCcm(ReadOnlySpan<byte> key)
        {
            AesAEAD.CheckKeySize(key.Length * 8);
            ImportKey(key);
        }

        public AesCcm(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            AesAEAD.CheckKeySize(key.Length * 8);
            ImportKey(key);
        }

        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[] associatedData = null)
        {
            AesAEAD.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
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
            EncryptInternal(nonce, plaintext, ciphertext, tag, associatedData);
        }

        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[] associatedData = null)
        {
            AesAEAD.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
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
            DecryptInternal(nonce, ciphertext, tag, plaintext, associatedData);
        }

        private static void CheckParameters(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag)
        {
            if (plaintext.Length != ciphertext.Length)
                throw new ArgumentException(SR.Cryptography_PlaintextCiphertextLengthMismatch);

            if (!nonce.Length.IsLegalSize(NonceByteSizes))
                throw new ArgumentException(SR.Cryptography_InvalidNonceLength, nameof(nonce));

            if (!tag.Length.IsLegalSize(TagByteSizes))
                throw new ArgumentException(SR.Cryptography_InvalidTagLength, nameof(tag));
        }
    }
}
