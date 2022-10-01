// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class AesGcm
    {
        private SafeEvpCipherCtxHandle _ctxHandle;

        public static bool IsSupported { get; } = Interop.OpenSslNoInit.OpenSslIsAvailable;

        [MemberNotNull(nameof(_ctxHandle))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            _ctxHandle = Interop.Crypto.EvpCipherCreatePartial(AesGcmOpenSslCommon.GetCipher(key.Length * 8));

            Interop.Crypto.CheckValidOpenSslHandle(_ctxHandle);
            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                key,
                Span<byte>.Empty,
                Interop.Crypto.EvpCipherDirection.NoChange);
            Interop.Crypto.EvpCipherSetGcmNonceLength(_ctxHandle, NonceSize);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            AesGcmOpenSslCommon.Encrypt(_ctxHandle, nonce, plaintext, ciphertext, tag, associatedData);
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            AesGcmOpenSslCommon.Decrypt(_ctxHandle, nonce, ciphertext, tag, plaintext, associatedData);
        }

        public void Dispose()
        {
            _ctxHandle.Dispose();
        }
    }
}
