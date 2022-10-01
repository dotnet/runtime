// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class AesGcm
    {
        // Apple CryptoKit does not support short authentication tags. Since .NET originally supported AES-GCM via
        // OpenSSL, which does support short tags, we need to continue to support them. If a caller supplies a short
        // tag we will continue to use OpenSSL if it is available. Otherwise, use CryptoKit.
        private static readonly bool s_openSslAvailable = Interop.OpenSslNoInit.OpenSslIsAvailable;
        private const int CryptoKitSupportedTagSizeInBytes = 16;

        private byte[]? _key;

        // CryptoKit added ChaCha20Poly1305 in macOS 10.15, which is our minimum target for macOS. We still may end
        // up throwing a platform not supported if a caller uses a short authentication tag and OpenSSL is not
        // available. But recommended use of AES-GCM with a 16-byte tag is supported.
        public static bool IsSupported => true;

        [MemberNotNull(nameof(_key))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // We should only be calling this in the constructor, so there shouldn't be a previous key.
            Debug.Assert(_key is null);

            // Pin the array on the POH so that the GC doesn't move it around to allow zeroing to be more effective.
            _key = GC.AllocateArray<byte>(key.Length, pinned: true);
            key.CopyTo(_key);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            CheckDisposed();

            if (tag.Length != CryptoKitSupportedTagSizeInBytes)
            {
                using (SafeEvpCipherCtxHandle ctxHandle = CreateOpenSslHandle())
                {
                    AesGcmOpenSslCommon.Encrypt(ctxHandle, nonce, plaintext, ciphertext, tag, associatedData);
                }
            }
            else
            {
                Interop.AppleCrypto.AesGcmEncrypt(
                    _key,
                    nonce,
                    plaintext,
                    ciphertext,
                    tag,
                    associatedData);
            }
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            CheckDisposed();

            if (tag.Length != CryptoKitSupportedTagSizeInBytes)
            {
                using (SafeEvpCipherCtxHandle ctxHandle = CreateOpenSslHandle())
                {
                    AesGcmOpenSslCommon.Decrypt(ctxHandle, nonce, ciphertext, tag, plaintext, associatedData);
                }
            }
            else
            {
                Interop.AppleCrypto.AesGcmDecrypt(
                    _key,
                    nonce,
                    ciphertext,
                    tag,
                    plaintext,
                    associatedData);
            }
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        [MemberNotNull(nameof(_key))]
        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_key is null, this);
        }

        private SafeEvpCipherCtxHandle CreateOpenSslHandle()
        {
            Debug.Assert(_key is not null);

            // We should only get here if the tag size is not 128-bit. If that happens, and OpenSSL is not available,
            // then we can't proceed.
            if (!s_openSslAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_AesGcmTagSize);
            }

            IntPtr cipherHandle = AesGcmOpenSslCommon.GetCipher(_key.Length * 8);
            SafeEvpCipherCtxHandle ctxHandle = Interop.Crypto.EvpCipherCreatePartial(cipherHandle);

            Interop.Crypto.CheckValidOpenSslHandle(ctxHandle);
            Interop.Crypto.EvpCipherSetKeyAndIV(
                ctxHandle,
                _key,
                Span<byte>.Empty,
                Interop.Crypto.EvpCipherDirection.NoChange);
            Interop.Crypto.EvpCipherSetGcmNonceLength(ctxHandle, NonceSize);
            return ctxHandle;
        }
    }
}
