// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
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
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = GetAlgorithm(cipherMode, feedbackSize);

            return new OpenSslCipherLite(
                algorithm,
                cipherMode,
                blockSize,
                paddingSize,
                key,
                iv,
                encrypting);
        }

        private static IntPtr GetAlgorithm(CipherMode cipherMode, int feedbackSizeInBytes) => cipherMode switch
            {
                CipherMode.CBC => Interop.Crypto.EvpDes3Cbc(),
                CipherMode.ECB => Interop.Crypto.EvpDes3Ecb(),
                CipherMode.CFB when feedbackSizeInBytes == 1 => Interop.Crypto.EvpDes3Cfb8(),
                CipherMode.CFB when feedbackSizeInBytes == 8 => Interop.Crypto.EvpDes3Cfb64(),
                _ => throw new NotSupportedException(),
            };
    }
}
