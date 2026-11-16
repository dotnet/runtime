// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation
    {
        private static readonly bool s_hasCryptoKitKeyWrap =
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsIOSVersionAtLeast(15) ||
            OperatingSystem.IsTvOSVersionAtLeast(15) ||
            OperatingSystem.IsMacCatalystVersionAtLeast(15);

        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> key,
            byte[]? iv,
            int blockSize,
            int paddingSize,
            int feedbackSizeInBytes,
            bool encrypting)
        {
            BasicSymmetricCipher cipher = new AppleCCCryptor(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.AES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting,
                feedbackSizeInBytes,
                paddingSize);

            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        private static AppleCCCryptorLite CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            int feedbackSizeInBytes,
            bool encrypting)
        {
            return new AppleCCCryptorLite(
                Interop.AppleCrypto.PAL_SymmetricAlgorithm.AES,
                cipherMode,
                blockSize,
                key,
                iv,
                encrypting,
                feedbackSizeInBytes,
                paddingSize);
        }

        protected override void EncryptKeyWrapCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!s_hasCryptoKitKeyWrap)
            {
                base.EncryptKeyWrapCore(source, destination);
                return;
            }

            FixedMemoryKeyBox keyBox = GetKey();
            bool addedRef = false;

            try
            {
                keyBox.DangerousAddRef(ref addedRef);
                int written = Interop.AppleCrypto.AesKeyWrapEncrypt(keyBox.DangerousKeySpan, source, destination);

                if (written != destination.Length)
                {
                    Debug.Fail($"CryptoKit wrote {written} bytes; expected {destination.Length}.");
                    throw new CryptographicException();
                }
            }
            finally
            {
                if (addedRef)
                {
                    keyBox.DangerousRelease();
                }
            }
        }

        protected override int DecryptKeyWrapCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!s_hasCryptoKitKeyWrap)
            {
                return base.DecryptKeyWrapCore(source, destination);
            }

            FixedMemoryKeyBox keyBox = GetKey();
            bool addedRef = false;

            try
            {
                keyBox.DangerousAddRef(ref addedRef);
                int written = Interop.AppleCrypto.AesKeyWrapDecrypt(keyBox.DangerousKeySpan, source, destination);

                if (written != destination.Length)
                {
                    Debug.Fail($"CryptoKit wrote {written} bytes; expected {destination.Length}.");
                    throw new CryptographicException();
                }

                return written;
            }
            finally
            {
                if (addedRef)
                {
                    keyBox.DangerousRelease();
                }
            }
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
                    feedbackSizeInBytes: 0,
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
                    feedbackSizeInBytes: 0,
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
