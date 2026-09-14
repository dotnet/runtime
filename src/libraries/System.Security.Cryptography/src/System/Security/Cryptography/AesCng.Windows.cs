// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is one of a group of files (AesCng.cs, TripleDESCng.cs) that are almost identical except
// for the algorithm name. If you make a change to this file, there's a good chance you'll have to make
// the same change to the other files so please check. This is a pain but given that the contracts demand
// that each of these derive from a different class, it can't be helped.
//

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    public sealed class AesCng : Aes, ICngSymmetricAlgorithm
    {
        private CngKey? _key;
        private ILiteSymmetricCipher? _encryptEcbCipher;
        private ILiteSymmetricCipher? _decryptEcbCipher;
        private ILiteSymmetricCipher? _encryptCbcCipher;
        private ILiteSymmetricCipher? _decryptCbcCipher;
        private ConcurrencyBlock _block;

        [SupportedOSPlatform("windows")]
        public AesCng()
        {
            _core = new CngSymmetricAlgorithmCore(this);
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName)
            : this(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider)
        {
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName, CngProvider provider)
            : this(keyName, provider, CngKeyOpenOptions.None)
        {
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            _core = new CngSymmetricAlgorithmCore(this, keyName, provider, openOptions);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AesCng"/> class with the specified <see cref="CngKey"/>.
        /// </summary>
        /// <param name="key">
        ///   The key that will be used as input to the cryptographic operations performed by the current object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="key"/> does not represent an AES key.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     An error occured while performing a cryptographic operation.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Cryptography Next Generation (CNG) is not supported on this system.
        /// </exception>
        [SupportedOSPlatform("windows")]
        public AesCng(CngKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            CngKey duplicate = CngHelpers.Duplicate(key.HandleNoDuplicate, key.IsEphemeral);
            _core = new CngSymmetricAlgorithmCore(this, duplicate);
            _key = duplicate;
        }

        public override byte[] Key
        {
            get
            {
                return _core.GetKeyIfExportable();
            }
            set
            {
                using (ConcurrencyBlock.Enter(ref _block))
                {
                    _core.SetKey(value);
                    ClearCachedCiphers();
                }
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
                using (ConcurrencyBlock.Enter(ref _block))
                {
                    _core.SetKeySize(value, this);
                    ClearCachedCiphers();
                }
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
            using (ConcurrencyBlock.Enter(ref _block))
            {
                _core.GenerateKey();
                ClearCachedCiphers();
            }
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
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = _core.CreateLiteSymmetricCipher(
                    iv,
                    encrypting: false,
                    CipherMode.CFB,
                    feedbackSizeInBits);

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
            using (ConcurrencyBlock.Enter(ref _block))
            {
                ILiteSymmetricCipher cipher = _core.CreateLiteSymmetricCipher(
                    iv,
                    encrypting: true,
                    CipherMode.CFB,
                    feedbackSizeInBits);

                using (cipher)
                {
                    return UniversalCryptoOneShot.OneShotEncrypt(cipher, paddingMode, plaintext, destination, out bytesWritten);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearCachedCiphers();

                if (_key is not null)
                {
                    _key.Dispose();
                    _key = null;
                }
            }

            base.Dispose(disposing);
        }

        byte[] ICngSymmetricAlgorithm.BaseKey
        {
            get
            {
                KeyValue ??= RandomNumberGenerator.GetBytes(AsymmetricAlgorithmHelpers.BitsToBytes(KeySizeValue));
                return KeyValue.CloneByteArray()!;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                long bitLength = value.Length;
                bitLength *= 8;
                if (bitLength > int.MaxValue || !ValidKeySize((int)bitLength))
                {
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                }

                KeySizeValue = (int)bitLength;
                KeyValue = value.CloneByteArray();
            }
        }

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
            return Cng.BCRYPT_AES_ALGORITHM;
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

            cipher = _core.CreateLiteSymmetricCipher(
                iv,
                encrypting,
                cipherMode,
                feedbackSizeInBits: 0);

            return cipher;
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
    }
}
