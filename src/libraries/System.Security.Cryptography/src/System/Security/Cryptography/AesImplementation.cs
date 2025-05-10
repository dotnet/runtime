// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        private FixedMemoryKeyBox? _keyBox;

        private FixedMemoryKeyBox GetKey()
        {
            if (_keyBox is null)
            {
                GenerateKey();
                Debug.Assert(_keyBox is not null);
            }

            return _keyBox;
        }

        public override byte[] Key
        {
            get => GetKey().UseKey("", static (_, key) => key.ToArray());
            set => SetKey(value);
        }

        public override int KeySize
        {
            get => base.KeySize;
            set
            {
                base.KeySize = value;
                _keyBox?.Dispose();
                _keyBox = null;
            }
        }

        public sealed override ICryptoTransform CreateDecryptor()
        {
            return GetKey().UseKey(
                this,
                static (instance, key) => instance.CreateTransform(key, instance.IV, encrypting: false));
        }

        public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public sealed override ICryptoTransform CreateEncryptor()
        {
            return GetKey().UseKey(
                this,
                static (instance, key) => instance.CreateTransform(key, instance.IV, encrypting: true));
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
            Span<byte> key = stackalloc byte[KeySize / BitsPerByte];
            RandomNumberGenerator.Fill(key);
            SetKeyCore(key);
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyBox?.Dispose();
                _keyBox = null;
            }

            base.Dispose(disposing);
        }

        protected override void SetKeyCore(ReadOnlySpan<byte> key)
        {
            KeySizeValue = checked(BitsPerByte * key.Length);
            _keyBox?.Dispose();
            _keyBox = new FixedMemoryKeyBox(key);
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            ILiteSymmetricCipher cipher = GetKey().UseKey(
                BlockSize / BitsPerByte,
                static (blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.ECB,
                    key,
                    iv: default,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    0, /*feedback size */
                    encrypting: false));

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
            ILiteSymmetricCipher cipher = GetKey().UseKey(
                BlockSize / BitsPerByte,
                static (blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.ECB,
                    key,
                    iv: default,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    0, /*feedback size */
                    encrypting: true));

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
            ILiteSymmetricCipher cipher = GetKey().UseKey(
                iv,
                BlockSize / BitsPerByte,
                static (iv, blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.CBC,
                    key,
                    iv,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    0, /*feedback size */
                    encrypting: true));

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
            ILiteSymmetricCipher cipher = GetKey().UseKey(
                iv,
                BlockSize / BitsPerByte,
                static (iv, blockSizeBytes, key) => CreateLiteCipher(
                    CipherMode.CBC,
                    key,
                    iv,
                    blockSize: blockSizeBytes,
                    paddingSize: blockSizeBytes,
                    0, /*feedback size */
                    encrypting: false));

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
            ValidateCFBFeedbackSize(feedbackSizeInBits);

            ILiteSymmetricCipher cipher = GetKey().UseKey(
                iv,
                (BlockSizeBytes: BlockSize / BitsPerByte, FeedbackSizeBytes: feedbackSizeInBits / BitsPerByte),
                static (iv, state, key) => CreateLiteCipher(
                    CipherMode.CFB,
                    key,
                    iv: iv,
                    blockSize: state.BlockSizeBytes,
                    paddingSize: state.FeedbackSizeBytes,
                    state.FeedbackSizeBytes,
                    encrypting: false));

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

            ILiteSymmetricCipher cipher = GetKey().UseKey(
                iv,
                (BlockSizeBytes: BlockSize / BitsPerByte, FeedbackSizeBytes: feedbackSizeInBits / BitsPerByte),
                static (iv, state, key) => CreateLiteCipher(
                    CipherMode.CFB,
                    key,
                    iv,
                    blockSize: state.BlockSizeBytes,
                    paddingSize: state.FeedbackSizeBytes,
                    state.FeedbackSizeBytes,
                    encrypting: true));

            using (cipher)
            {
                return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
            }
        }

        private UniversalCryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            ArgumentNullException.ThrowIfNull(rgbKey);

            return CreateTransform(new ReadOnlySpan<byte>(rgbKey), rgbIV, encrypting);
        }

        private UniversalCryptoTransform CreateTransform(ReadOnlySpan<byte> rgbKey, byte[]? rgbIV, bool encrypting)
        {
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

        private const int BitsPerByte = 8;
    }
}
