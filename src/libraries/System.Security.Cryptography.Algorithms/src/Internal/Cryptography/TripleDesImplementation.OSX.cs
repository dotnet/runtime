// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal partial class TripleDesImplementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            bool encrypting)
        {
            BasicSymmetricCipher cipher = new AppleCCCryptor(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.TripleDES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }
    }
}
