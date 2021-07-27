// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        public sealed override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public sealed override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
        }

        public sealed override void GenerateIV()
        {
            IV = RandomNumberGenerator.GetBytes(BlockSize / BitsPerByte);
        }

        public sealed override void GenerateKey()
        {
            Key = RandomNumberGenerator.GetBytes(KeySize / BitsPerByte);
        }

        protected sealed override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.ECB,
                paddingMode,
                Key,
                iv: null,
                blockSize: BlockSize / BitsPerByte,
                paddingSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                encrypting: false);

            using (transform)
            {
                return transform.TransformOneShot(ciphertext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptEcbCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.ECB,
                paddingMode,
                Key,
                iv: null,
                blockSize: BlockSize / BitsPerByte,
                paddingSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                encrypting: true);

            using (transform)
            {
                return transform.TransformOneShot(plaintext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptCbcCore(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.CBC,
                paddingMode,
                Key,
                iv: iv.ToArray(),
                blockSize: BlockSize / BitsPerByte,
                paddingSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                encrypting: true);

            using (transform)
            {
                return transform.TransformOneShot(plaintext, destination, out bytesWritten);
            }
        }

        protected override bool TryDecryptCbcCore(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.CBC,
                paddingMode,
                Key,
                iv: iv.ToArray(),
                blockSize: BlockSize / BitsPerByte,
                paddingSize: BlockSize / BitsPerByte,
                0, /*feedback size */
                encrypting: false);

            using (transform)
            {
                return transform.TransformOneShot(ciphertext, destination, out bytesWritten);
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
            ValidateCFBFeedbackSize(feedbackSizeInBits);

            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.CFB,
                paddingMode,
                Key,
                iv: iv.ToArray(),
                blockSize: BlockSize / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                feedbackSizeInBits / BitsPerByte,
                encrypting: false);

            using (transform)
            {
                return transform.TransformOneShot(ciphertext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptCfbCore(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            int feedbackSizeInBits,
            out int bytesWritten)
        {
            ValidateCFBFeedbackSize(feedbackSizeInBits);

            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.CFB,
                paddingMode,
                Key,
                iv: iv.ToArray(),
                blockSize: BlockSize / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                feedbackSizeInBits / BitsPerByte,
                encrypting: true);

            using (transform)
            {
                return transform.TransformOneShot(plaintext, destination, out bytesWritten);
            }
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            // note: rbgIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(this.LegalKeySizes))
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

            return CreateTransformCore(
                Mode,
                Padding,
                rgbKey,
                rgbIV,
                BlockSize / BitsPerByte,
                this.GetPaddingSize(Mode, FeedbackSize),
                FeedbackSize / BitsPerByte,
                encrypting);
        }

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // only 8bits/128bits feedback would be valid.
            if (feedback != 8 && feedback != 128)
            {
                throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
            }
        }

        private const int BitsPerByte = 8;
    }
}
