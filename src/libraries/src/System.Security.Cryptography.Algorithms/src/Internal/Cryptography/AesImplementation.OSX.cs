// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            BasicSymmetricCipher cipher = new AppleCCCryptor(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.AES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
