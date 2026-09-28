// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;
using Internal.NativeCrypto;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    public partial class AesGcm
    {
        private SafeKeyHandle _keyHandle;
        private static readonly KeySizes s_tagByteSizes = new KeySizes(12, 16, 1);

        public static partial bool IsSupported => true;

        public static partial KeySizes TagByteSizes => s_tagByteSizes;

        [MemberNotNull(nameof(_keyHandle))]
        private partial void ImportKey(ReadOnlySpan<byte> key)
        {
            _keyHandle = Interop.BCrypt.BCryptImportKey(BCryptAeadHandleCache.AesGcm, key);
        }

        private partial void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            AeadCommon.Encrypt(_keyHandle, nonce, associatedData, plaintext, ciphertext, tag);
        }

        private partial void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            AeadCommon.Decrypt(_keyHandle, nonce, associatedData, ciphertext, tag, plaintext, clearPlaintextOnFailure: true);
        }

        public partial void Dispose()
        {
            _keyHandle.Dispose();
        }
    }
}
