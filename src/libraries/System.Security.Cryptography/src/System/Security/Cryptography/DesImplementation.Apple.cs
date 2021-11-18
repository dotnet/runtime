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
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            BasicSymmetricCipher cipher = new AppleCCCryptor(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.DES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting,
                feedbackSizeInBytes,
                paddingSize);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            return new AppleCCCryptorLite(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.DES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting,
                feedbackSizeInBytes,
                paddingSize);
        }
    }
}
