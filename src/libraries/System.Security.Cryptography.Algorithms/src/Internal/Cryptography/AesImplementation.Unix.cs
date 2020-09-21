// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal partial class AesImplementation
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

        private static readonly Tuple<int, int, CipherMode, Func<IntPtr>>[] s_algorithmInitializers =
        {
            // Neither OpenSSL nor Cng Aes support CTS mode.
            // second parameter is feedback size (required only for CFB).

            Tuple.Create(128, 0, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes128Cbc),
            Tuple.Create(128, 0, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes128Ecb),
            Tuple.Create(128, 8, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes128Cfb8),
            Tuple.Create(128, 128, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes128Cfb128),

            Tuple.Create(192, 0, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes192Cbc),
            Tuple.Create(192, 0, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes192Ecb),
            Tuple.Create(192, 8, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes192Cfb8),
            Tuple.Create(192, 128, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes192Cfb128),

            Tuple.Create(256, 0, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes256Cbc),
            Tuple.Create(256, 0, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes256Ecb),
            Tuple.Create(256, 8, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes256Cfb8),
            Tuple.Create(256, 128, CipherMode.CFB, (Func<IntPtr>)Interop.Crypto.EvpAes256Cfb128),
        };

        private static IntPtr GetAlgorithm(int keySize, int feedback, CipherMode cipherMode)
        {
            bool foundKeysize = false;

            foreach (var tuple in s_algorithmInitializers)
            {
                if (tuple.Item1 == keySize && (tuple.Item2 == 0 || tuple.Item2 == feedback) && tuple.Item3 == cipherMode)
                {
                    return tuple.Item4();
                }

                if (tuple.Item1 == keySize)
                {
                    foundKeysize = true;
                }
            }

            if (!foundKeysize)
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }

            throw new NotSupportedException();
        }
    }
}
