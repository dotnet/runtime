// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for DES, not consuming it.")]
    internal sealed partial class DesImplementation : DES
    {
        private const int BitsPerByte = 8;
        private ILiteSymmetricCipher? _encryptCbcLiteHash, _decryptCbcLiteHash, _encryptEcbLiteHash, _decryptEcbLiteHash;

        public DesImplementation()
        {
            // Default CFB to CFB8. .NET Framework uses 8 as the default for DESCryptoServiceProvider which
            // was used for DES.Create(), and .NET doesn't support anything other than 8 for the feedback size for DES,
            // so also default it to the only value that works.
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
            byte[] key = new byte[KeySize / BitsPerByte];
            RandomNumberGenerator.Fill(key);
            // Never hand back a weak or semi-weak key
            while (IsWeakKey(key) || IsSemiWeakKey(key))
            {
                RandomNumberGenerator.Fill(key);
            }

            Key = key;
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

        private UniversalCryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            ArgumentNullException.ThrowIfNull(rgbKey);

            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

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
                iv,
                blockSize: BlockSize / BitsPerByte,
                feedbackSizeInBits / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
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
                feedbackSizeInBits / BitsPerByte,
                paddingSize: feedbackSizeInBits / BitsPerByte,
                encrypting: true);

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
            }
        }

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // only 8bits feedback is available on all platforms
            if (feedback != 8)
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
                    0, /*feedback size */
                    paddingSize: BlockSize / BitsPerByte,
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
