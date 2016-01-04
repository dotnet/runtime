// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    partial class TripleDesImplementation
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
            IntPtr algorithm;
            switch (cipherMode)
            {
                case CipherMode.CBC:
                    algorithm = Interop.Crypto.EvpDes3Cbc();
                    break;
                case CipherMode.ECB:
                    algorithm = Interop.Crypto.EvpDes3Ecb();
                    break;
                default:
                    throw new NotSupportedException();
            }

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, key, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------    
    }
}
