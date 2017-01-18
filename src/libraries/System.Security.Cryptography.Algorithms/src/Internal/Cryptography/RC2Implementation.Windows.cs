// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Diagnostics;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    partial class RC2Implementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            int effectiveKeyLength,
            byte[] iv,
            int blockSize,
            bool encrypting)
        {
            using (SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, effectiveKeyLength))
            {
                // The BasicSymmetricCipherBCrypt ctor will increase algorithm reference count and take ownership.
                BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, key, true, iv, encrypting);
                return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
            }
        }
    }
}
