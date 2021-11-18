// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ChaCha20Poly1305
    {
        private SafeEvpCipherCtxHandle _ctxHandle;

        public static bool IsSupported { get; } = Interop.Crypto.CipherIsSupported(Interop.Crypto.EvpChaCha20Poly1305());

        [MemberNotNull(nameof(_ctxHandle))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // Constructors should check key size before calling ImportKey.
            Debug.Assert(key.Length == KeySizeInBytes);
            _ctxHandle = Interop.Crypto.EvpCipherCreatePartial(Interop.Crypto.EvpChaCha20Poly1305());

            Interop.Crypto.CheckValidOpenSslHandle(_ctxHandle);
            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                key,
                Span<byte>.Empty,
                Interop.Crypto.EvpCipherDirection.NoChange);

            Interop.Crypto.CipherSetNonceLength(_ctxHandle, NonceSizeInBytes);
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

            if (!associatedData.IsEmpty)
            {
                Interop.Crypto.CipherUpdateAAD(_ctxHandle, associatedData);
            }

            byte[]? rented = null;
            int ciphertextAndTagLength = checked(ciphertext.Length + tag.Length);

            try
            {
                // Arbitrary limit.
                const int StackAllocMax = 128;
                Span<byte> ciphertextAndTag = stackalloc byte[StackAllocMax];

                if (ciphertextAndTagLength > StackAllocMax)
                {
                    rented = CryptoPool.Rent(ciphertextAndTagLength);
                    ciphertextAndTag = rented;
                }

                ciphertextAndTag = ciphertextAndTag.Slice(0, ciphertextAndTagLength);

                if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, ciphertextAndTag, out int ciphertextBytesWritten, plaintext))
                {
                    throw new CryptographicException();
                }

                if (!Interop.Crypto.EvpCipherFinalEx(
                    _ctxHandle,
                    ciphertextAndTag.Slice(ciphertextBytesWritten),
                    out int bytesWritten))
                {
                    throw new CryptographicException();
                }

                ciphertextBytesWritten += bytesWritten;

                // NOTE: Android appends tag to the end of the ciphertext in case of ChaCha20Poly1305 and "encryption" mode

                if (ciphertextBytesWritten != ciphertextAndTagLength)
                {
                    Debug.Fail($"ChaCha20Poly1305 encrypt wrote {ciphertextBytesWritten} of {ciphertextAndTagLength} bytes.");
                    throw new CryptographicException();
                }

                ciphertextAndTag.Slice(0, ciphertext.Length).CopyTo(ciphertext);
                ciphertextAndTag.Slice(ciphertext.Length).CopyTo(tag);
            }
            finally
            {
                if (rented is not null)
                {
                    CryptoPool.Return(rented, clearSize: ciphertextAndTagLength);
                }
            }
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

            if (!associatedData.IsEmpty)
            {
                Interop.Crypto.CipherUpdateAAD(_ctxHandle, associatedData);
            }

            if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, plaintext, out int plaintextBytesWritten, ciphertext))
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new CryptographicException();
            }

            if (!Interop.Crypto.EvpCipherUpdate(_ctxHandle, plaintext.Slice(plaintextBytesWritten), out int bytesWritten, tag))
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new CryptographicException();
            }

            plaintextBytesWritten += bytesWritten;

            if (!Interop.Crypto.EvpCipherFinalEx(
                _ctxHandle,
                plaintext.Slice(plaintextBytesWritten),
                out bytesWritten))
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new CryptographicException(SR.Cryptography_AuthTagMismatch);
            }

            plaintextBytesWritten += bytesWritten;

            if (plaintextBytesWritten != plaintext.Length)
            {
                Debug.Fail($"ChaCha20Poly1305 decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                throw new CryptographicException();
            }
        }

        public void Dispose()
        {
            _ctxHandle.Dispose();
        }
    }
}
