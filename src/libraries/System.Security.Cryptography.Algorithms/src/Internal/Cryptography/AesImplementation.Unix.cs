// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedback,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(key.Length * 8, feedback * 8, cipherMode);

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, 0, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
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
    }
}
