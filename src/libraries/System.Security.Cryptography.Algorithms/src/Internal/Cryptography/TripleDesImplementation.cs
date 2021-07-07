// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for TripleDES, not consuming it.")]
    internal sealed partial class TripleDesImplementation : TripleDES
    {
        private const int BitsPerByte = 8;

        public TripleDesImplementation()
        {
            // Default CFB to CFB8 to match .NET Framework's default for TripleDES.Create()
            // and TripleDESCryptoServiceProvider.
            FeedbackSizeValue = 8;
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

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (rgbIV != null)
            {
                long ivSize = rgbIV.Length * (long)BitsPerByte;
                if (ivSize != BlockSize)
                    throw new ArgumentException(SR.Cryptography_InvalidIVSize, nameof(rgbIV));
            }

            if (rgbKey.Length == 16)
            {
                // Some platforms do not support Two-Key Triple DES, so manually support it here.
                // Two-Key Triple DES contains two 8-byte keys {K1}{K2} with {K1} appended to make {K1}{K2}{K1}.
                byte[] newkey = new byte[24];
                Array.Copy(rgbKey, newkey, 16);
                Array.Copy(rgbKey, 0, newkey, 16, 8);
                rgbKey = newkey;
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

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // only 8bits/64bits feedback would be valid.
            if (feedback != 8 && feedback != 64)
            {
                throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
            }
        }
    }
}
