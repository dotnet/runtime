// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class RC2Implementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int _ /*feedbackSizeInBytes*/,
            int paddingSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode);

            Interop.Crypto.EnsureLegacyAlgorithmsRegistered();

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode);

            Interop.Crypto.EnsureLegacyAlgorithmsRegistered();
            return new OpenSslCipherLite(algorithm, blockSize, paddingSize, key, iv, encrypting);
        }

        private static IntPtr GetAlgorithm(CipherMode cipherMode) => cipherMode switch
            {
                CipherMode.CBC => Interop.Crypto.EvpRC2Cbc(),
                CipherMode.ECB => Interop.Crypto.EvpRC2Ecb(),
                _ => throw new NotSupportedException(),
            };
    }
}
