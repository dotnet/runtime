// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class DesImplementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int feedbackSize,
            int paddingSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode, feedbackSize);

            BasicSymmetricCipher cipher = new OpenSslCipher(algorithm, cipherMode, blockSize, paddingSize, key, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int feedbackSize,
            int paddingSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode, feedbackSize);
            return new OpenSslCipherLite(algorithm, cipherMode, blockSize, paddingSize, key, iv, encrypting);
        }

        private static IntPtr GetAlgorithm(CipherMode cipherMode, int feedbackSize)
        {
            return cipherMode switch
            {
                CipherMode.CBC => Interop.Crypto.EvpDesCbc(),
                CipherMode.ECB => Interop.Crypto.EvpDesEcb(),
                CipherMode.CFB when feedbackSize == 1 => Interop.Crypto.EvpDesCfb8(),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
