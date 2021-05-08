// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
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
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            if (!ValidKeySize(rgbKey.Length, out int keySize))
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

            int effectiveKeySize = EffectiveKeySizeValue == 0 ? keySize : EffectiveKeySize;
            return CreateTransformCore(Mode, Padding, rgbKey, effectiveKeySize, rgbIV, BlockSize / BitsPerByte, FeedbackSize / BitsPerByte, GetPaddingSize(), encrypting);
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            if (!ValidKeySize(Key.Length, out int keySize))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            int effectiveKeySize = EffectiveKeySizeValue == 0 ? keySize : EffectiveKeySize;
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.ECB,
                paddingMode,
                Key,
                effectiveKeyLength: effectiveKeySize,
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
            if (!ValidKeySize(Key.Length, out int keySize))
                throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);

            int effectiveKeySize = EffectiveKeySizeValue == 0 ? keySize : EffectiveKeySize;
            UniversalCryptoTransform transform = CreateTransformCore(
                CipherMode.ECB,
                paddingMode,
                Key,
                effectiveKeyLength: effectiveKeySize,
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

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // CFB not supported at all
            throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
        }

        private int GetPaddingSize()
        {
            return BlockSize / BitsPerByte;
        }

        private bool ValidKeySize(int keySizeBytes, out int keySizeBits)
        {
            if (keySizeBytes > (int.MaxValue / BitsPerByte))
            {
                keySizeBits = 0;
                return false;
            }

            keySizeBits = keySizeBytes << 3;
            return keySizeBits.IsLegalSize(LegalKeySizes);
        }
    }
}
