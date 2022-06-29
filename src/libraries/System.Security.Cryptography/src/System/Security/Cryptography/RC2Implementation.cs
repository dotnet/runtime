// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for RC2, not consuming it.")]
    internal sealed partial class RC2Implementation : RC2
    {
        private const int BitsPerByte = 8;

        public override int EffectiveKeySize
        {
            get
            {
                return KeySizeValue;
            }
            set
            {
                if (value != KeySizeValue)
                    throw new CryptographicUnexpectedOperationException(SR.Cryptography_RC2_EKSKS2);
            }
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
        }

        public override void GenerateIV()
        {
            IV = RandomNumberGenerator.GetBytes(BlockSize / BitsPerByte);
        }

        public sealed override void GenerateKey()
        {
            Key = RandomNumberGenerator.GetBytes(KeySize / BitsPerByte);
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            ArgumentNullException.ThrowIfNull(rgbKey);

            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (!ValidKeySize(rgbKey.Length))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (rgbIV != null)
            {
                long ivSize = rgbIV.Length * (long)BitsPerByte;
                if (ivSize != BlockSize)
                    throw new ArgumentException(SR.Cryptography_InvalidIVSize, nameof(rgbIV));
            }

            if (Mode == CipherMode.CFB)
            {
                ValidateCFBFeedbackSize(FeedbackSize);
            }

            Debug.Assert(EffectiveKeySize == KeySize);
            return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / BitsPerByte, FeedbackSize / BitsPerByte, GetPaddingSize(), encrypting);
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            if (!ValidKeySize(Key.Length))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            Debug.Assert(EffectiveKeySize == KeySize);
            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.ECB,
                paddingMode,
                Key,
                iv: null,
                blockSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
                encrypting: false);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotDecrypt(cipher, paddingMode, ciphertext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptEcbCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            if (!ValidKeySize(Key.Length))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            Debug.Assert(EffectiveKeySize == KeySize);
            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.ECB,
                paddingMode,
                Key,
                iv: default,
                blockSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
                encrypting: true);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptCbcCore(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            if (!ValidKeySize(Key.Length))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            Debug.Assert(EffectiveKeySize == KeySize);
            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.CBC,
                paddingMode,
                Key,
                iv,
                blockSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
                encrypting: true);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
            }
        }

        protected override bool TryDecryptCbcCore(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            if (!ValidKeySize(Key.Length))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            Debug.Assert(EffectiveKeySize == KeySize);
            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.CBC,
                paddingMode,
                Key,
                iv,
                blockSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
                encrypting: false);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotDecrypt(cipher, paddingMode, ciphertext, destination, out bytesWritten);
            }
        }

        protected override bool TryDecryptCfbCore(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            int feedbackSizeInBits,
            out int bytesWritten)
        {
            throw new CryptographicException(SR.Format(SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
        }

        protected override bool TryEncryptCfbCore(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            int feedbackSizeInBits,
            out int bytesWritten)
        {
            throw new CryptographicException(SR.Format(SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
        }

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // CFB not supported at all
            throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
        }

        private int GetPaddingSize()
        {
            return BlockSize / BitsPerByte;
        }

        private new bool ValidKeySize(int keySizeBytes)
        {
            if (keySizeBytes > (int.MaxValue / BitsPerByte))
            {
                return false;
            }

            int keySizeBits = keySizeBytes << 3;
            return keySizeBits.IsLegalSize(LegalKeySizes);
        }
    }
}
