// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        private FixedMemoryKeyBox? _keyBox;
        private ILiteSymmetricCipher? _encryptEcbCipher;
        private ILiteSymmetricCipher? _decryptEcbCipher;
        private ILiteSymmetricCipher? _encryptCbcCipher;
        private ILiteSymmetricCipher? _decryptCbcCipher;
        private ConcurrencyBlock _block;

        private FixedMemoryKeyBox GetKey()
        {
            if (_keyBox is null)
            {
                Span<byte> key = stackalloc byte[KeySize / BitsPerByte];

                try
                {
                    RandomNumberGenerator.Fill(key);
                    SetKeyCoreUnchecked(key);
                    Debug.Assert(_keyBox is not null);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key);
                }
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
                using (ConcurrencyBlock.Enter(ref _block))
                {
                    base.KeySize = value;
                    ClearCachedCiphers();
                    _keyBox?.Dispose();
                    _keyBox = null;
                }
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

        public sealed override unsafe void GenerateKey()
        {
            Span<byte> key = stackalloc byte[KeySize / BitsPerByte];

            try
            {
                RandomNumberGenerator.Fill(key);
                SetKeyCore(key);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearCachedCiphers();
                _keyBox?.Dispose();
                _keyBox = null;
            }

            base.Dispose(disposing);
        }

        protected override void SetKeyCore(ReadOnlySpan<byte> key)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                SetKeyCoreUnchecked(key);
            }
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = GetOrCreateCachedLiteCipher(
                    ref _decryptEcbCipher,
                    CipherMode.ECB,
                    iv: default,
                    encrypting: false);

                return UniversalCryptoOneShot.OneShotDecrypt(cipher, paddingMode, ciphertext, destination, out bytesWritten);
            }
        }

        protected override bool TryEncryptEcbCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = GetOrCreateCachedLiteCipher(
                    ref _encryptEcbCipher,
                    CipherMode.ECB,
                    iv: default,
                    encrypting: true);

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
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = GetOrCreateCachedLiteCipher(
                    ref _encryptCbcCipher,
                    CipherMode.CBC,
                    iv,
                    encrypting: true);

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
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = GetOrCreateCachedLiteCipher(
                    ref _decryptCbcCipher,
                    CipherMode.CBC,
                    iv,
                    encrypting: false);

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

            using (ConcurrencyBlock.Enter(ref _block))
            {
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

            using (ConcurrencyBlock.Enter(ref _block))
            {
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

        private ILiteSymmetricCipher GetOrCreateCachedLiteCipher(
            ref ILiteSymmetricCipher? cipher,
            CipherMode cipherMode,
            ReadOnlySpan<byte> iv,
            bool encrypting)
        {
            Debug.Assert(cipherMode is CipherMode.ECB or CipherMode.CBC);

            if (cipher is not null)
            {
                try
                {
                    cipher.Reset(iv);
                    return cipher;
                }
                catch
                {
                    cipher.Dispose();
                    cipher = null; // Null-out the cipher field passed by reference.
                    throw;
                }
            }

            int blockSizeBytes = BlockSize / BitsPerByte;
            cipher = GetKey().UseKey(
                iv,
                (BlockSizeBytes: blockSizeBytes, CipherMode: cipherMode, Encrypting: encrypting),
                static (iv, state, key) => CreateLiteCipher(
                    state.CipherMode,
                    key,
                    iv,
                    blockSize: state.BlockSizeBytes,
                    paddingSize: state.BlockSizeBytes,
                    0, /* feedback size */
                    encrypting: state.Encrypting));

            return cipher;
        }

        private void SetKeyCoreUnchecked(ReadOnlySpan<byte> key)
        {
            KeySizeValue = checked(BitsPerByte * key.Length);
            FixedMemoryKeyBox keyBox = new FixedMemoryKeyBox(key);
            ClearCachedCiphers();
            _keyBox?.Dispose();
            _keyBox = keyBox;
        }

        private void ClearCachedCiphers()
        {
            _encryptEcbCipher?.Dispose();
            _encryptEcbCipher = null;
            _decryptEcbCipher?.Dispose();
            _decryptEcbCipher = null;
            _encryptCbcCipher?.Dispose();
            _encryptCbcCipher = null;
            _decryptCbcCipher?.Dispose();
            _decryptCbcCipher = null;
        }

        private const int BitsPerByte = 8;
    }
}
