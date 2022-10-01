// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static class AesGcmOpenSslCommon
    {
        internal static void Encrypt(
            SafeEvpCipherCtxHandle ctxHandle,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            Interop.Crypto.EvpCipherSetKeyAndIV(
                ctxHandle,
                Span<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Encrypt);

            if (associatedData.Length != 0)
            {
                if (!Interop.Crypto.EvpCipherUpdate(ctxHandle, Span<byte>.Empty, out _, associatedData))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }

            if (!Interop.Crypto.EvpCipherUpdate(ctxHandle, ciphertext, out int ciphertextBytesWritten, plaintext))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            if (!Interop.Crypto.EvpCipherFinalEx(
                ctxHandle,
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

            Interop.Crypto.EvpCipherGetGcmTag(ctxHandle, tag);
        }

        internal static void Decrypt(
            SafeEvpCipherCtxHandle ctxHandle,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            Interop.Crypto.EvpCipherSetKeyAndIV(
                ctxHandle,
                ReadOnlySpan<byte>.Empty,
                nonce,
                Interop.Crypto.EvpCipherDirection.Decrypt);

            if (associatedData.Length != 0)
            {
                if (!Interop.Crypto.EvpCipherUpdate(ctxHandle, Span<byte>.Empty, out _, associatedData))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }

            if (!Interop.Crypto.EvpCipherUpdate(ctxHandle, plaintext, out int plaintextBytesWritten, ciphertext))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            Interop.Crypto.EvpCipherSetGcmTag(ctxHandle, tag);

            if (!Interop.Crypto.EvpCipherFinalEx(
                ctxHandle,
                plaintext.Slice(plaintextBytesWritten),
                out int bytesWritten))
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw new AuthenticationTagMismatchException();
            }

            plaintextBytesWritten += bytesWritten;

            if (plaintextBytesWritten != plaintext.Length)
            {
                Debug.Fail($"GCM decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                throw new CryptographicException();
            }
        }

        internal static IntPtr GetCipher(int keySizeInBits)
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
    }
}
