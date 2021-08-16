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
            _ctxHandle = Interop.Crypto.EvpCipherCreatePartial(GetCipher(key.Length * 8));

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
            ReadOnlySpan<byte> associatedData = default)
        {
            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                Span<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Encrypt);

            if (associatedData.Length != 0)
            {
                if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, Span<byte>.Empty, out _, associatedData))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }

            if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, ciphertext, out int ciphertextBytesWritten, plaintext))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            if (!Interop.Crypto.EvpCipherFinalEx(
                _ctxHandle,
                ciphertext.Slice(ciphertextBytesWritten),
                out int bytesWritten))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            ciphertextBytesWritten += bytesWritten;

            if (ciphertextBytesWritten != ciphertext.Length)
            {
                Debug.Fail($"GCM encrypt wrote {ciphertextBytesWritten} of {ciphertext.Length} bytes.");
                throw new CryptographicException();
            }

            Interop.Crypto.EvpCipherGetGcmTag(_ctxHandle, tag);
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                ReadOnlySpan<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Decrypt);

            if (associatedData.Length != 0)
            {
                if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, Span<byte>.Empty, out _, associatedData))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }

            if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, plaintext, out int plaintextBytesWritten, ciphertext))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            Interop.Crypto.EvpCipherSetGcmTag(_ctxHandle, tag);

            if (!Interop.Crypto.EvpCipherFinalEx(
                _ctxHandle,
                plaintext.Slice(plaintextBytesWritten),
                out int bytesWritten))
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new CryptographicException(SR.Cryptography_AuthTagMismatch);
            }

            plaintextBytesWritten += bytesWritten;

            if (plaintextBytesWritten != plaintext.Length)
            {
                Debug.Fail($"GCM decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                throw new CryptographicException();
            }
        }

        private static IntPtr GetCipher(int keySizeInBits)
        {
            switch (keySizeInBits)
            {
                case 128: return Interop.Crypto.EvpAes128Gcm();
                case 192: return Interop.Crypto.EvpAes192Gcm();
                case 256: return Interop.Crypto.EvpAes256Gcm();
                default:
                    Debug.Fail("Key size should already be validated");
                    return IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            _ctxHandle.Dispose();
        }
    }
}
