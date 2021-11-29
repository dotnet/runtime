// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is one of a group of files (AesCng.cs, TripleDESCng.cs) that are almost identical except
// for the algorithm name. If you make a change to this file, there's a good chance you'll have to make
// the same change to the other files so please check. This is a pain but given that the contracts demand
// that each of these derive from a different class, it can't be helped.
//

using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    public sealed class AesCng : Aes, ICngSymmetricAlgorithm
    {
        public AesCng()
        {
            _core = new CngSymmetricAlgorithmCore(this);
        }

        public AesCng(string keyName)
            : this(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider)
        {
        }

        public AesCng(string keyName, CngProvider provider)
            : this(keyName, provider, CngKeyOpenOptions.None)
        {
        }

        public AesCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            _core = new CngSymmetricAlgorithmCore(this, keyName, provider, openOptions);
        }

        public override byte[] Key
        {
            get
            {
                return _core.GetKeyIfExportable();
            }
            set
            {
                _core.SetKey(value);
            }
        }

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }

            set
            {
                _core.SetKeySize(value, this);
            }
        }

        public override ICryptoTransform CreateDecryptor()
        {
            // Do not change to CreateDecryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateDecryptor();
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return _core.CreateDecryptor(rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            // Do not change to CreateEncryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateEncryptor();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return _core.CreateEncryptor(rgbKey, rgbIV);
        }

        public override void GenerateKey()
        {
            _core.GenerateKey();
        }

        public override void GenerateIV()
        {
            _core.GenerateIV();
        }

        protected override bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: null,
                encrypting: false,
                paddingMode,
                CipherMode.ECB,
                feedbackSizeInBits: 0);

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
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: null,
                encrypting: true,
                paddingMode,
                CipherMode.ECB,
                feedbackSizeInBits: 0);

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
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: iv.ToArray(),
                encrypting: true,
                paddingMode,
                CipherMode.CBC,
                feedbackSizeInBits: 0);

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
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: iv.ToArray(),
                encrypting: false,
                paddingMode,
                CipherMode.CBC,
                feedbackSizeInBits: 0);

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
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: iv.ToArray(),
                encrypting: false,
                paddingMode,
                CipherMode.CFB,
                feedbackSizeInBits);

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
            UniversalCryptoTransform transform = _core.CreateCryptoTransform(
                iv: iv.ToArray(),
                encrypting: true,
                paddingMode,
                CipherMode.CFB,
                feedbackSizeInBits);

            using (transform)
            {
                return transform.TransformOneShot(plaintext, destination, out bytesWritten);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        byte[] ICngSymmetricAlgorithm.BaseKey { get { return base.Key; } set { base.Key = value; } }
        int ICngSymmetricAlgorithm.BaseKeySize { get { return base.KeySize; } set { base.KeySize = value; } }

        bool ICngSymmetricAlgorithm.IsWeakKey(byte[] key)
        {
            return false;
        }

        int ICngSymmetricAlgorithm.GetPaddingSize(CipherMode mode, int feedbackSizeBits)
        {
            return this.GetPaddingSize(mode, feedbackSizeBits);
        }

        SafeAlgorithmHandle ICngSymmetricAlgorithm.GetEphemeralModeHandle(CipherMode mode, int feedbackSizeInBits)
        {
            try
            {
                return AesBCryptModes.GetSharedHandle(mode, feedbackSizeInBits / 8);
            }
            catch (NotSupportedException)
            {
                throw new CryptographicException(SR.Cryptography_InvalidCipherMode);
            }
        }

        string ICngSymmetricAlgorithm.GetNCryptAlgorithmIdentifier()
        {
            return Interop.NCrypt.NCRYPT_AES_ALGORITHM;
        }

        byte[] ICngSymmetricAlgorithm.PreprocessKey(byte[] key)
        {
            return key;
        }

        bool ICngSymmetricAlgorithm.IsValidEphemeralFeedbackSize(int feedbackSizeInBits)
        {
            return feedbackSizeInBits == 8 || feedbackSizeInBits == 128;
        }

        private CngSymmetricAlgorithmCore _core;
    }
}
