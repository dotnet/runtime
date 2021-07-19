// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for DES, not consuming it.")]
    internal sealed partial class DesImplementation : DES
    {
        private const int BitsPerByte = 8;

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
            byte[] key = new byte[KeySize / BitsPerByte];
            RandomNumberGenerator.Fill(key);
            // Never hand back a weak or semi-weak key
            while (IsWeakKey(key) || IsSemiWeakKey(key))
            {
                RandomNumberGenerator.Fill(key);
            }
            KeyValue = key;
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (IsWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_Weak, "DES");
            if (IsSemiWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_SemiWeak, "DES");

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
                FeedbackSize / BitsPerByte,
                this.GetPaddingSize(Mode, FeedbackSize),
                encrypting);
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
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
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
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
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
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
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
                0, /*feedback size */
                paddingSize: BlockSize / BitsPerByte,
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
                feedbackSizeInBits / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
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
                feedbackSizeInBits / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                encrypting: true);

            using (transform)
            {
                return transform.TransformOneShot(plaintext, destination, out bytesWritten);
            }
        }

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // only 8bits feedback is available on all platforms
            if (feedback != 8)
            {
                throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
            }
        }
    }
}
