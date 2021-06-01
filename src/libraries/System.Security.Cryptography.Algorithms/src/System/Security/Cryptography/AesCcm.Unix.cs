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

        private void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            using (SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreatePartial(GetCipher(_key.Length * 8)))
            {
                Interop.Crypto.CheckValidOpenSslHandle(ctx);

                // We need to set mode to encryption before setting the tag and nonce length
                // otherwise older versions of OpenSSL (i.e. 1.0.1f which can be found on Ubuntu 14.04) will fail
                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, Span<byte>.Empty, Span<byte>.Empty, Interop.Crypto.EvpCipherDirection.Encrypt);
                Interop.Crypto.EvpCipherSetCcmTagLength(ctx, tag.Length);
                Interop.Crypto.EvpCipherSetCcmNonceLength(ctx, nonce.Length);
                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.NoChange);

                if (associatedData.Length != 0)
                {
                    // length needs to be known ahead of time in CCM mode
                    Interop.Crypto.EvpCipherSetInputLength(ctx, plaintext.Length);

                    if (!Interop.Crypto.EvpCipherUpdate(ctx, Span<byte>.Empty, out _, associatedData))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                if (!Interop.Crypto.EvpCipherUpdate(ctx, ciphertext, out int ciphertextBytesWritten, plaintext))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                if (!Interop.Crypto.EvpCipherFinalEx(
                    ctx,
                    ciphertext.Slice(ciphertextBytesWritten),
                    out int bytesWritten))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                ciphertextBytesWritten += bytesWritten;

                if (ciphertextBytesWritten != ciphertext.Length)
                {
                    Debug.Fail($"CCM encrypt wrote {ciphertextBytesWritten} of {ciphertext.Length} bytes.");
                    throw new CryptographicException();
                }

                Interop.Crypto.EvpCipherGetCcmTag(ctx, tag);
            }
        }

        private void DecryptCore(
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
                Interop.Crypto.EvpCipherSetCcmTag(ctx, tag);

                Interop.Crypto.EvpCipherSetKeyAndIV(ctx, _key, nonce, Interop.Crypto.EvpCipherDirection.Decrypt);

                if (associatedData.Length != 0)
                {
                    // length needs to be known ahead of time in CCM mode
                    Interop.Crypto.EvpCipherSetInputLength(ctx, ciphertext.Length);

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

                if (plaintextBytesWritten != plaintext.Length)
                {
                    Debug.Fail($"CCM decrypt wrote {plaintextBytesWritten} of {plaintext.Length} bytes.");
                    throw new CryptographicException();
                }

                // The OpenSSL documentation says not to call EvpCipherFinalEx for CCM decryption, and calling it will report failure.
                // https://wiki.openssl.org/index.php/EVP_Authenticated_Encryption_and_Decryption#Authenticated_Decryption_using_CCM_mode
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
