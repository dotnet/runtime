// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        internal const int BlockSizeBytes = 16; // 128 bits

        // SubtleCrypto doesn't support AES-192. http://crbug.com/533699
        internal static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 128) };

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
            ValidateCipherMode(cipherMode);
            if (iv is null)
                throw new CryptographicException(SR.Cryptography_MissingIV);

            Debug.Assert(blockSize == BlockSizeBytes);
            Debug.Assert(paddingSize == blockSize);

            BasicSymmetricCipher cipher = Interop.BrowserCrypto.CanUseSubtleCrypto ?
                new AesSubtleCryptoTransform(key, iv, encrypting) :
                new AesManagedTransform(key, iv, encrypting);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
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

            Debug.Assert(blockSize == BlockSizeBytes);
            Debug.Assert(paddingSize == blockSize);

            return Interop.BrowserCrypto.CanUseSubtleCrypto ?
                new AesSubtleCryptoTransform(key, iv, encrypting) :
                new AesManagedTransform(key, iv, encrypting);
        }

        private static void ValidateCipherMode(CipherMode cipherMode)
        {
            if (cipherMode != CipherMode.CBC)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CipherModeBrowser);
        }
    }
}
