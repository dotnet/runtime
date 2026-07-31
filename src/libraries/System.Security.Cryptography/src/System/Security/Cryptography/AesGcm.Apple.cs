// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    public sealed partial class AesGcm
    {
        private SafeAppleCryptoSymmetricKeyHandle _key;

        // CryptoKit only supports 16 byte tags.
        private static readonly KeySizes s_tagByteSizes = new KeySizes(16, 16, 1);

        // CryptoKit added AES.GCM in macOS 10.15, and iOS/tvOS in 13.0.
        public static partial bool IsSupported => true;

        public static partial KeySizes TagByteSizes => s_tagByteSizes;

        [MemberNotNull(nameof(_key))]
        private partial void ImportKey(ReadOnlySpan<byte> key)
        {
            // We should only be calling this in the constructor, so there shouldn't be a previous key.
            Debug.Assert(_key is null);
            _key = Interop.AppleCrypto.SymmetricKeyImport(key);
        }

        private partial void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            Interop.AppleCrypto.AesGcmEncrypt(
                _key,
                nonce,
                plaintext,
                ciphertext,
                tag,
                associatedData);
        }

        private partial void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            Interop.AppleCrypto.AesGcmDecrypt(
                _key,
                nonce,
                ciphertext,
                tag,
                plaintext,
                associatedData);
        }

        public partial void Dispose() => _key.Dispose();
    }
}
