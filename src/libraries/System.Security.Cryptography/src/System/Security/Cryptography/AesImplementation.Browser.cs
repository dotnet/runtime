// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            ValidateCipherMode(cipherMode);
            ValidatePaddingMode(paddingMode);

            Debug.Assert(blockSize == AesManagedTransform.BlockSizeBytes);
            Debug.Assert(paddingSize == blockSize);

            return new AesManagedTransform(key, iv, encrypting);
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            ValidateCipherMode(cipherMode);

            Debug.Assert(blockSize == AesManagedTransform.BlockSizeBytes);
            Debug.Assert(paddingSize == blockSize);

            return new AesManagedTransform(key, iv, encrypting);
        }

        private static void ValidateCipherMode(CipherMode cipherMode)
        {
            if (cipherMode != CipherMode.CBC)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CipherModeBrowser);
        }

        internal static void ValidatePaddingMode(PaddingMode paddingMode)
        {
            if (paddingMode != PaddingMode.PKCS7)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_PaddingModeBrowser);
        }
    }
}
