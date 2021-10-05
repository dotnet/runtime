// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

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
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode);

            Interop.Crypto.EnsureLegacyAlgorithmsRegistered();

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, effectiveKeyLength, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
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
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode);

            Interop.Crypto.EnsureLegacyAlgorithmsRegistered();
            return new OpenSslCipherLite(algorithm, cipherMode, blockSize, paddingSize, key, effectiveKeyLength, iv, encrypting);
        }

        private static IntPtr GetAlgorithm(CipherMode cipherMode) => cipherMode switch
            {
                CipherMode.CBC => Interop.Crypto.EvpRC2Cbc(),
                CipherMode.ECB => Interop.Crypto.EvpRC2Ecb(),
                _ => throw new NotSupportedException(),
            };
    }
}
