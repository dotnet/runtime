// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            byte[] iv,
            int blockSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(key.Length * 8, cipherMode);

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, key, 0, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static readonly Tuple<int, CipherMode, Func<IntPtr>>[] s_algorithmInitializers =
        {
            // Neither OpenSSL nor Cng Aes support CTS mode.
            // Cng Aes doesn't seem to support CFB mode, and that would
            // require passing in the feedback size.  Since Windows doesn't support it,
            // we can skip it here, too.
            Tuple.Create(128, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes128Cbc),
            Tuple.Create(128, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes128Ecb),

            Tuple.Create(192, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes192Cbc),
            Tuple.Create(192, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes192Ecb),

            Tuple.Create(256, CipherMode.CBC, (Func<IntPtr>)Interop.Crypto.EvpAes256Cbc),
            Tuple.Create(256, CipherMode.ECB, (Func<IntPtr>)Interop.Crypto.EvpAes256Ecb),
        };

        private static IntPtr GetAlgorithm(int keySize, CipherMode cipherMode)
        {
            bool foundKeysize = false;

            foreach (var triplet in s_algorithmInitializers)
            {
                if (triplet.Item1 == keySize && triplet.Item2 == cipherMode)
                {
                    return triplet.Item3();
                }

                if (triplet.Item1 == keySize)
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
