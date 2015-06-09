// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal partial class AesImplementation
    {
        private static ICryptoTransform CreateEncryptor(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            int blockSize)
        {
            return new AesOpenSslCryptoTransform(cipherMode, paddingMode, key, iv, blockSize, true);
        }

        private static ICryptoTransform CreateDecryptor(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            int blockSize)
        {
            return new AesOpenSslCryptoTransform(cipherMode, paddingMode, key, iv, blockSize, false);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
