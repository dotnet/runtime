// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class AesCcm
    {
        private byte[]? _key;

        public static bool IsSupported => true;

        [MemberNotNull(nameof(_key))]
        private void ImportKey(ReadOnlySpan<byte> key)
        {
            // Pin the array on the POH so that the GC doesn't move it around to allow zeroing to be more effective.
            _key = GC.AllocateArray<byte>(key.Length, pinned: true);
            key.CopyTo(_key);
        }

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            CheckDisposed();

            // Convert key length to bits.
            using (SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreatePartial(GetCipher(_key.Length * 8)))
            {
                if (ctx.IsInvalid)
                {
                    throw new CryptographicException();
                }

                if (!Interop.Crypto.CipherSetTagLength(ctx, tag.Length))
                {
                    throw new CryptographicException();
                }

                Interop.Crypto.CipherSetNonceLength(ctx, nonce.Length);
                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.Encrypt);

                if (associatedData.Length != 0)
                {
                    Interop.Crypto.CipherUpdateAAD(ctx, associatedData);
                }

                byte[]? rented = null;
                try
                {
                    scoped Span<byte> ciphertextAndTag;

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

                    if (!Interop.Crypto.EvpCipherUpdate(ctx, ciphertextAndTag, out int ciphertextBytesWritten, plaintext))
                    {
                        throw new CryptographicException();
                    }

                    if (!Interop.Crypto.EvpAeadCipherFinalEx(
                        ctx,
                        ciphertextAndTag.Slice(ciphertextBytesWritten),
                        out int bytesWritten,
                        out bool authTagMismatch))
                    {
                        Debug.Assert(!authTagMismatch);
                        throw new CryptographicException();
                    }

                    ciphertextBytesWritten += bytesWritten;

                    // NOTE: Android appends tag to the end of the ciphertext in case of CCM/GCM and "encryption" mode

                    if (ciphertextBytesWritten != ciphertextAndTag.Length)
                    {
                        Debug.Fail($"CCM encrypt wrote {ciphertextBytesWritten} of {ciphertextAndTag.Length} bytes.");
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
        }

        private void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            CheckDisposed();

            using (SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreatePartial(GetCipher(_key.Length * 8)))
            {
                if (ctx.IsInvalid)
                {
                    throw new CryptographicException();
                }
                Interop.Crypto.CipherSetNonceLength(ctx, nonce.Length);

                if (!Interop.Crypto.CipherSetTagLength(ctx, tag.Length))
                {
                    throw new CryptographicException();
                }

                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.Decrypt);

                if (associatedData.Length != 0)
                {
                    Interop.Crypto.CipherUpdateAAD(ctx, associatedData);
                }

                if (!Interop.Crypto.EvpCipherUpdate(ctx, plaintext, out int plaintextBytesWritten, ciphertext))
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    throw new CryptographicException();
                }

                if (!Interop.Crypto.EvpCipherUpdate(ctx, plaintext.Slice(plaintextBytesWritten), out int bytesWritten, tag))
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    throw new CryptographicException();
                }

                plaintextBytesWritten += bytesWritten;

                if (!Interop.Crypto.EvpAeadCipherFinalEx(
                    ctx,
                    plaintext.Slice(plaintextBytesWritten),
                    out bytesWritten,
                    out bool authTagMismatch))
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (authTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }

                    throw new CryptographicException(SR.Arg_CryptographyException);
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
            return keySizeInBits switch
            {
                128 => Interop.Crypto.EvpAes128Ccm(),
                192 => Interop.Crypto.EvpAes192Ccm(),
                256 => Interop.Crypto.EvpAes256Ccm(),
                _ => IntPtr.Zero
            };
        }

        [MemberNotNull(nameof(_key))]
        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_key is null, this);
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }
    }
}
