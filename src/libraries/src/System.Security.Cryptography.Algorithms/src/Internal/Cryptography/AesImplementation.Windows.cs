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
            return new AesCngCryptoEncryptor(cipherMode, paddingMode, key, iv, blockSize);
        }

        private static ICryptoTransform CreateDecryptor(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            int blockSize)
        {
            return new AesCngCryptoDecryptor(cipherMode, paddingMode, key, iv, blockSize);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
