// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal partial class RC2Implementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            int effectiveKeyLength,
            byte[]? iv,
            int blockSize,
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            BasicSymmetricCipher cipher = new AppleCCCryptor(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.RC2,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting,
                feedbackSizeInBytes,
                paddingSize);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }
    }
}
