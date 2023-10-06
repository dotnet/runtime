// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        private const int BitsPerByte = 8;

        private ILiteSymmetricCipher? _encryptCbcLiteHash;
        private ILiteSymmetricCipher? _decryptCbcLiteHash;
        private ILiteSymmetricCipher? _encryptEcbLiteHash;
        private ILiteSymmetricCipher? _decryptEcbLiteHash;

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
            InvalidateOneShotAlgorithms();
            base.Dispose(disposing);
        }

        public override byte[] Key
        {
            set
            {
                base.Key = value;
                InvalidateOneShotAlgorithms();
            }
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            return OneShotTransformation(
                ref _decryptEcbLiteHash,
                CipherMode.ECB,
                iv: default,
                encrypting: false,
                paddingMode,
                ciphertext,
                destination,
                UniversalCryptoOneShot.OneShotDecrypt,
                out bytesWritten);
        }

        protected override bool TryEncryptEcbCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            return OneShotTransformation(
                ref _encryptEcbLiteHash,
                CipherMode.ECB,
                iv: default,
                encrypting: true,
                paddingMode,
                plaintext,
                destination,
                UniversalCryptoOneShot.OneShotEncrypt,
                out bytesWritten);
        }

        protected override bool TryEncryptCbcCore(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            return OneShotTransformation(
                ref _encryptCbcLiteHash,
                CipherMode.CBC,
                iv,
                encrypting: true,
                paddingMode,
                plaintext,
                destination,
                UniversalCryptoOneShot.OneShotEncrypt,
                out bytesWritten);
        }

        protected override bool TryDecryptCbcCore(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> iv,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            return OneShotTransformation(
                ref _decryptCbcLiteHash,
                CipherMode.CBC,
                iv,
                encrypting: false,
                paddingMode,
                ciphertext,
                destination,
                UniversalCryptoOneShot.OneShotDecrypt,
                out bytesWritten);
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

            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.CFB,
                Key,
                iv: iv,
                blockSize: BlockSize / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                feedbackSizeInBits / BitsPerByte,
                encrypting: false);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotDecrypt(cipher, paddingMode, ciphertext, destination, out bytesWritten);
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

            ILiteSymmetricCipher cipher = CreateLiteCipher(
                CipherMode.CFB,
                Key,
                iv,
                blockSize: BlockSize / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                feedbackSizeInBits / BitsPerByte,
                encrypting: true);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
            }
        }

        private UniversalCryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            ArgumentNullException.ThrowIfNull(rgbKey);

            // note: rbgIV is guaranteed to be cloned before this method, so no need to clone it again

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
                throw new CryptographicException(SR.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
            }
        }

        private bool OneShotTransformation(
            ref ILiteSymmetricCipher? cipherCache,
            CipherMode cipherMode,
            ReadOnlySpan<byte> iv,
            bool encrypting,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            UniversalCryptoOneShot.UniversalOneShotCallback callback,
            out int bytesWritten)
        {
            // Ensures we have a zero feedback size.
            Debug.Assert(cipherMode is CipherMode.CBC or CipherMode.ECB);

            // Try grabbing a cached instance.
            ILiteSymmetricCipher? cipher = Interlocked.Exchange(ref cipherCache, null);

            if (cipher is null)
            {
                // If there is no cached instance available, create one. This also sets the initialization vector during creation.
                cipher = CreateLiteCipher(
                    cipherMode,
                    Key,
                    iv,
                    blockSize: BlockSize / BitsPerByte,
                    paddingSize: BlockSize / BitsPerByte,
                    0, /*feedback size */
                    encrypting);
            }
            else
            {
                // If we did grab a cached instance, put it back in to a working state. This needs to happen even for ECB
                // since lite ciphers do not reset after the final transformation automatically.
                cipher.Reset(iv);
            }

            bool result = callback(cipher, paddingMode, source, destination, out bytesWritten);

            // Try making this instance available to use again later. If another thread put one there, dispose of it.
            cipher = Interlocked.Exchange(ref cipherCache, cipher);
            cipher?.Dispose();

            return result;
        }

        private void InvalidateOneShotAlgorithms()
        {
            Interlocked.Exchange(ref _encryptCbcLiteHash, null)?.Dispose();
            Interlocked.Exchange(ref _decryptCbcLiteHash, null)?.Dispose();
            Interlocked.Exchange(ref _encryptEcbLiteHash, null)?.Dispose();
            Interlocked.Exchange(ref _decryptEcbLiteHash, null)?.Dispose();
        }
    }
}
