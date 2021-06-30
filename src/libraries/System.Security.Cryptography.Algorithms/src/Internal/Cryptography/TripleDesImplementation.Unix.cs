// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed partial class TripleDesImplementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm;
            switch ((cipherMode, feedbackSize))
            {
                case (CipherMode.CBC, _):
                    algorithm = Interop.Crypto.EvpDes3Cbc();
                    break;
                case (CipherMode.ECB, _):
                    algorithm = Interop.Crypto.EvpDes3Ecb();
                    break;
                case (CipherMode.CFB, 1):
                    algorithm = Interop.Crypto.EvpDes3Cfb8();
                    break;
                case (CipherMode.CFB, 8):
                    algorithm = Interop.Crypto.EvpDes3Cfb64();
                    break;
                default:
                    throw new NotSupportedException();
            }

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, 0, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }
    }
}
