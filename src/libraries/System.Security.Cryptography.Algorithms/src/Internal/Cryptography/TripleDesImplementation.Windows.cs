// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Internal.NativeCrypto;

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
            SafeAlgorithmHandle algorithm = TripleDesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);

            BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, paddingSize, key, false, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }
    }
}
