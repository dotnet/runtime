// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class AesCcm
    {
        private byte[] _key;

        [MemberNotNull(nameof(_key))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // OpenSSL does not allow setting nonce length after setting the key
            // we need to store it as bytes instead
            _key = key.ToArray();
        }

        private void EncryptInternal(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            using (SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreatePartial(GetCipher(_key.Length * 8)))
            {
                Interop.Crypto.CheckValidOpenSslHandle(ctx);

                if (!Interop.Crypto.EvpCipherSetTagLength(ctx, tag.Length))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                Interop.Crypto.EvpCipherSetCcmNonceLength(ctx, nonce.Length);
                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.Encrypt);

                if (associatedData.Length != 0)
                {
                    if (!Interop.Crypto.EvpCipherUpdateAAD(ctx, associatedData))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                byte[]? rented = null;
                try
                {
                    Span<byte> ciphertextAndTag = stackalloc byte[0];
                    // Arbitrary limit.
                    const int StackAllocMax = 128;
                    if (ciphertext.Length + tag.Length <= StackAllocMax)
                    {
                        ciphertextAndTag = stackalloc byte[ciphertext.Length + tag.Length];
                    }
                    else
                    {
                        rented = CryptoPool.Rent(ciphertext.Length + tag.Length);
                        ciphertextAndTag = new Span<byte>(rented, 0, ciphertext.Length + tag.Length);
                    }

                    if (!Interop.Crypto.EvpCipherUpdate(ctx, ciphertextAndTag, out int ciphertextBytesWritten, plaintext))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    if (!Interop.Crypto.EvpCipherFinalEx(
                        ctx,
                        ciphertextAndTag.Slice(ciphertextBytesWritten),
                        out int bytesWritten))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    ciphertextBytesWritten += bytesWritten;

                    // NOTE: Android appends tag to the end of the ciphertext in case of CCM/GCM and "encryption" mode

                    if (ciphertextBytesWritten != ciphertextAndTag.Length)
                    {
                        Debug.Fail($"CCM encrypt wrote {ciphertextBytesWritten} of {ciphertextAndTag.Length} bytes.");
                        throw new CryptographicException();
                    }

                    ciphertextAndTag.Slice(0, ciphertext.Length).CopyTo(ciphertext);
                    ciphertextAndTag.Slice(ciphertext.Length).CopyTo(tag);
                }
                finally
                {
                    if (rented != null)
                    {
                        CryptoPool.Return(rented, ciphertext.Length + tag.Length);
                    }
                }
            }
        }

        private void DecryptInternal(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            using (SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreatePartial(GetCipher(_key.Length * 8)))
            {
                Interop.Crypto.CheckValidOpenSslHandle(ctx);
                Interop.Crypto.EvpCipherSetCcmNonceLength(ctx, nonce.Length);
                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.Decrypt);

                if (associatedData.Length != 0)
                {
                    if (!Interop.Crypto.EvpCipherUpdate(ctx, Span<byte>.Empty, out _, associatedData))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                if (!Interop.Crypto.EvpCipherUpdate(ctx, plaintext, out int plaintextBytesWritten, ciphertext))
                {
                    plaintext.Clear();
                    throw new CryptographicException(SR.Cryptography_AuthTagMismatch);
                }

                if (!Interop.Crypto.EvpCipherUpdate(ctx, plaintext.Slice(plaintextBytesWritten), out int bytesWritten, tag))
                {
                    plaintext.Clear();
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                plaintextBytesWritten += bytesWritten;

                if (!Interop.Crypto.EvpCipherFinalEx(
                    ctx,
                    plaintext.Slice(plaintextBytesWritten),
                    out bytesWritten))
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    throw new CryptographicException(SR.Cryptography_AuthTagMismatch);
                }

                plaintextBytesWritten += bytesWritten;

                if (plaintextBytesWritten != plaintext.Length)
                {
                    Debug.Fail($"CCM decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                    throw new CryptographicException();
                }
            }
        }

        private static IntPtr GetCipher(int keySizeInBits)
        {
            switch (keySizeInBits)
            {
                case 128: return Interop.Crypto.EvpAes128Ccm();
                case 192: return Interop.Crypto.EvpAes192Ccm();
                case 256: return Interop.Crypto.EvpAes256Ccm();
                default:
                    Debug.Fail("Key size should already be validated");
                    return IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_key);
        }
    }
}
