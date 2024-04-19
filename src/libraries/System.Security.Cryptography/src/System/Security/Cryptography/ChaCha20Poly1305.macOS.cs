// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public sealed partial class ChaCha20Poly1305
    {
        // CryptoKit added ChaCha20Poly1305 in macOS 10.15, which is our minimum target for macOS.
        public static bool IsSupported => true;
        private FixedMemoryKeyBox _keyBox;

        [MemberNotNull(nameof(_keyBox))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // We should only be calling this in the constructor, so there shouldn't be a previous key.
            Debug.Assert(_keyBox is null);
            _keyBox = new FixedMemoryKeyBox(key);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            bool acquired = false;

            try
            {
                _keyBox.DangerousAddRef(ref acquired);
                Interop.AppleCrypto.ChaCha20Poly1305Encrypt(
                    _keyBox.DangerousKeySpan,
                    nonce,
                    plaintext,
                    ciphertext,
                    tag,
                    associatedData);
            }
            finally
            {
                if (acquired)
                {
                    _keyBox.DangerousRelease();
                }
            }
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            bool acquired = false;

            try
            {
                _keyBox.DangerousAddRef(ref acquired);
                Interop.AppleCrypto.ChaCha20Poly1305Decrypt(
                    _keyBox.DangerousKeySpan,
                    nonce,
                    ciphertext,
                    tag,
                    plaintext,
                    associatedData);
            }
            finally
            {
                if (acquired)
                {
                    _keyBox.DangerousRelease();
                }
            }
        }

        public void Dispose() => _keyBox.Dispose();
    }
}
