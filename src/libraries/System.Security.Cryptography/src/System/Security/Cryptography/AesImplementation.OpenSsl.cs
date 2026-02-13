// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedback,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(key.Length * 8, feedback * 8, cipherMode);

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static OpenSslCipherLite CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            int feedback,
            bool encrypting)
        {
            IntPtr algorithm = GetAlgorithm(key.Length * 8, feedback * 8, cipherMode);
            return new OpenSslCipherLite(algorithm, blockSize, paddingSize, key, iv, encrypting);
        }

        protected override void EncryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            int written = KeyWrap(source, destination, enc: 1);
            Debug.Assert(written == destination.Length);
        }

        protected override int DecryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            return KeyWrap(source, destination, enc: 0);
        }

        private int KeyWrap(ReadOnlySpan<byte> source, Span<byte> destination, int enc)
        {
            Debug.Assert(enc is 0 or 1);

            SafeEvpCipherCtxHandle ctx = GetKey().UseKey(
                state: enc,
                static (enc, key) =>
                {
                    int keySizeInBits = key.Length * 8;

                    IntPtr algorithm = GetKeyWrapAlgorithm(keySizeInBits);

                    SafeEvpCipherCtxHandle ctx = Interop.Crypto.EvpCipherCreate(
                        algorithm,
                        ref MemoryMarshal.GetReference(key),
                        key.Length * 8,
                        ref MemoryMarshal.GetReference(ReadOnlySpan<byte>.Empty),
                        enc);

                    if (ctx.IsInvalid)
                    {
                        ctx.Dispose();
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    return ctx;
                });

            int written;

            using (ctx)
            {
                // OpenSSL AES-KWP requires that the destination be at least as large as the source length plus the block size.
                const int AesBlockSizeBytes = 16;
                using (CryptoPoolLease lease = CryptoPoolLease.RentConditionally(
                    checked(source.Length + AesBlockSizeBytes),
                    destination,
                    skipClearIfNotRented: true))
                {
                    bool ret = Interop.Crypto.EvpCipherUpdate(
                        ctx,
                        lease.Span,
                        out written,
                        source);

                    if (!ret)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    if (lease.IsRented)
                    {
                        if (written > destination.Length)
                        {
                            Debug.Fail("Wrote more bytes than expected");
                            throw new CryptographicException();
                        }

                        lease.Span.Slice(0, written).CopyTo(destination);
                    }

                    Debug.Assert(written > 0);
                }

                // Experimentation and code inspection show that EVP_CipherFinal_ex is not needed here,
                // the work is done in EVP_CipherUpdate.
                // Since AES-KW(P) involves multiple passes over the data, where the end of each pass
                // stores a tag/checksum back in the beginning of the buffer, it makes sense that only
                // one of Update or Final could write data, and they chose to go with Update.
                //
                // As the call to Final does not yield more data, and we're about to dispose the context,
                // don't bother making the call.
            }

            return written;
        }

        private static IntPtr GetAlgorithm(int keySize, int feedback, CipherMode cipherMode) =>
            (keySize, cipherMode) switch
            {
                // Neither OpenSSL nor Cng Aes support CTS mode.

                (128, CipherMode.CBC) => Interop.Crypto.EvpAes128Cbc(),
                (128, CipherMode.ECB) => Interop.Crypto.EvpAes128Ecb(),
                (128, CipherMode.CFB) when feedback == 8 => Interop.Crypto.EvpAes128Cfb8(),
                (128, CipherMode.CFB) when feedback == 128 => Interop.Crypto.EvpAes128Cfb128(),

                (192, CipherMode.CBC) => Interop.Crypto.EvpAes192Cbc(),
                (192, CipherMode.ECB) => Interop.Crypto.EvpAes192Ecb(),
                (192, CipherMode.CFB) when feedback == 8 => Interop.Crypto.EvpAes192Cfb8(),
                (192, CipherMode.CFB) when feedback == 128 => Interop.Crypto.EvpAes192Cfb128(),

                (256, CipherMode.CBC) => Interop.Crypto.EvpAes256Cbc(),
                (256, CipherMode.ECB) => Interop.Crypto.EvpAes256Ecb(),
                (256, CipherMode.CFB) when feedback == 8 => Interop.Crypto.EvpAes256Cfb8(),
                (256, CipherMode.CFB) when feedback == 128 => Interop.Crypto.EvpAes256Cfb128(),

                _ => throw (keySize == 128 || keySize == 192 || keySize == 256 ? (Exception)
                        new NotSupportedException() :
                        new CryptographicException(SR.Cryptography_InvalidKeySize)),
            };

        private static IntPtr GetKeyWrapAlgorithm(int keySize) =>
            keySize switch
            {
                128 => Interop.Crypto.EvpAes128WrapPad(),
                192 => Interop.Crypto.EvpAes192WrapPad(),
                256 => Interop.Crypto.EvpAes256WrapPad(),
                _ => throw new CryptographicException(SR.Cryptography_InvalidKeySize),
            };
    }
}
