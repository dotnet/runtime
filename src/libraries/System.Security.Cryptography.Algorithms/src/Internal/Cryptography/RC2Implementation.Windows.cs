// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Diagnostics;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed partial class RC2Implementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            int effectiveKeyLength,
            byte[]? iv,
            int blockSize,
            int feedbackSize,
            int paddingSize,
            bool encrypting)
        {
            using (SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, effectiveKeyLength))
            {
                // The BasicSymmetricCipherBCrypt ctor will increase algorithm reference count and take ownership.
                BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, paddingSize, key, true, iv, encrypting);
                return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
            }
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            int effectiveKeyLength,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            using (SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, effectiveKeyLength))
            {
                // The BasicSymmetricCipherBCrypt ctor will increase algorithm reference count and take ownership.
                return new BasicSymmetricCipherLiteBCrypt(
                    algorithm,
                    cipherMode,
                    blockSize,
                    paddingSize,
                    key,
                    ownsParentHandle: true,
                    iv,
                    encrypting);
            }
        }
    }
}
