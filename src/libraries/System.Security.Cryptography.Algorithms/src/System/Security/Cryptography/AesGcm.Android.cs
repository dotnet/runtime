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

        public static bool IsSupported => true;

        [MemberNotNull(nameof(_ctxHandle))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // Convert key length to bits.
            _ctxHandle = Interop.Crypto.EvpCipherCreatePartial(GetCipher(key.Length * 8));

            Interop.Crypto.CheckValidOpenSslHandle(_ctxHandle);
            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                key,
                Span<byte>.Empty,
                Interop.Crypto.EvpCipherDirection.NoChange);
            Interop.Crypto.CipherSetNonceLength(_ctxHandle, NonceSize);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {

            if (!Interop.Crypto.CipherSetTagLength(_ctxHandle, tag.Length))
            {
                throw new CryptographicException();
            }

            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                Span<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Encrypt);

            if (associatedData.Length != 0)
            {
                Interop.Crypto.CipherUpdateAAD(_ctxHandle, associatedData);
            }

            byte[]? rented = null;
            try
            {
                Span<byte> ciphertextAndTag = stackalloc byte[0];
                // Arbitrary limit.
                const int StackAllocMax = 128;
                if (checked(ciphertext.Length + tag.Length) <= StackAllocMax)
                {
                    ciphertextAndTag = stackalloc byte[ciphertext.Length + tag.Length];
                }
                else
                {
                    rented = CryptoPool.Rent(ciphertext.Length + tag.Length);
                    ciphertextAndTag = new Span<byte>(rented, 0, ciphertext.Length + tag.Length);
                }

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

                // NOTE: Android appends tag to the end of the ciphertext in case of CCM/GCM and "encryption" mode

                if (ciphertextBytesWritten != ciphertextAndTag.Length)
                {
                    Debug.Fail($"GCM encrypt wrote {ciphertextBytesWritten} of {ciphertextAndTag.Length} bytes.");
                    throw new CryptographicException();
                }

                ciphertextAndTag[..ciphertext.Length].CopyTo(ciphertext);
                ciphertextAndTag[ciphertext.Length..].CopyTo(tag);
            }
            finally
            {
                if (rented != null)
                {
                    CryptoPool.Return(rented, ciphertext.Length + tag.Length);
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
            if (!Interop.Crypto.CipherSetTagLength(_ctxHandle, tag.Length))
            {
                throw new CryptographicException();
            }

            Interop.Crypto.EvpCipherSetKeyAndIV(
                _ctxHandle,
                ReadOnlySpan<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Decrypt);

            if (associatedData.Length != 0)
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
                Debug.Fail($"GCM decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                throw new CryptographicException();
            }
        }

        private static IntPtr GetCipher(int keySizeInBits)
        {
            return keySizeInBits switch
            {
                 128 => Interop.Crypto.EvpAes128Gcm(),
                 192 => Interop.Crypto.EvpAes192Gcm(),
                 256 => Interop.Crypto.EvpAes256Gcm(),
                 _ => IntPtr.Zero
            };
        }

        public void Dispose()
        {
            _ctxHandle.Dispose();
        }
    }
}
