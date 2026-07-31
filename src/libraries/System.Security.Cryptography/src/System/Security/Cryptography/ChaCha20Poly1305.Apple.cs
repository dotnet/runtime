// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    public sealed partial class ChaCha20Poly1305
    {
        // CryptoKit added ChaCha20Poly1305 in macOS 10.15, and iOS/tvOS in 13.0.
        public static bool IsSupported => true;
        private SafeAppleCryptoSymmetricKeyHandle _key;

        [MemberNotNull(nameof(_key))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // We should only be calling this in the constructor, so there shouldn't be a previous key.
            Debug.Assert(_key is null);
            _key = Interop.AppleCrypto.SymmetricKeyImport(key);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            Interop.AppleCrypto.ChaCha20Poly1305Encrypt(
                _key,
                nonce,
                plaintext,
                ciphertext,
                tag,
                associatedData);
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            Interop.AppleCrypto.ChaCha20Poly1305Decrypt(
                _key,
                nonce,
                ciphertext,
                tag,
                plaintext,
                associatedData);
        }

        public void Dispose() => _key.Dispose();
    }
}
