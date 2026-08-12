// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            SafeAlgorithmHandle algorithm = AesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);

            BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, paddingSize, key, false, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static BasicSymmetricCipherLiteBCrypt CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            int feedbackSize,
            bool encrypting)
        {
            SafeAlgorithmHandle algorithm = AesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);

            return new BasicSymmetricCipherLiteBCrypt(
                algorithm,
                blockSize,
                paddingSize,
                key,
                ownsParentHandle: false,
                iv,
                encrypting);
        }

        protected override void EncryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Debug.Assert(destination.Length == GetKeyWrapPaddedLength(source.Length));

            ILiteSymmetricCipher cipher = GetKey().UseKey(
                BlockSize / BitsPerByte,
                static (blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.ECB,
                    key,
                    iv: default,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    feedbackSize: 0,
                    encrypting: true));

            using (cipher)
            {
                EncryptKeyWrapPaddedCore(
                    source,
                    destination,
                    cipher,
                    static (cipher, source, destination) => cipher.Transform(source, destination));
            }
        }

        protected override int DecryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            ILiteSymmetricCipher cipher = GetKey().UseKey(
                BlockSize / BitsPerByte,
                static (blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.ECB,
                    key,
                    iv: default,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    feedbackSize: 0,
                    encrypting: false));

            using (cipher)
            {
                return DecryptKeyWrapPaddedCore(
                    source,
                    destination,
                    cipher,
                    static (cipher, source, destination) => cipher.Transform(source, destination));
            }
        }
    }
}
