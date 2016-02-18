// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for crypto symmetric algorithms that use Cng.
    //
    internal struct CngSymmetricAlgorithmCore
    {
        /// <summary>
        /// Configures the core to use plaintext keys (to be auto-generated when first needed.)
        /// </summary>
        public CngSymmetricAlgorithmCore(string algorithm, ICngSymmetricAlgorithm outer)
        {
            _algorithm = algorithm;
            _outer = outer;

            _keyName = null; // Setting _keyName to null signifies that this object is based on a plaintext key, not a stored CNG key.
            _provider = null;
            _optionOptions = CngKeyOpenOptions.None;
        }

        /// <summary>
        /// Constructs the core to use a stored CNG key. 
        /// </summary>
        public CngSymmetricAlgorithmCore(string algorithm, ICngSymmetricAlgorithm outer, string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            if (keyName == null)
                throw new ArgumentNullException(nameof(keyName));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _algorithm = algorithm;
            _outer = outer;

            _keyName = keyName;
            _provider = provider;
            _optionOptions = openOptions;

            using (CngKey cngKey = ProduceCngKey())
            {
                CngAlgorithm actualAlgorithm = cngKey.Algorithm;
                if (algorithm != actualAlgorithm.Algorithm)
                    throw new CryptographicException(SR.Format(SR.Cryptography_CngKeyWrongAlgorithm, actualAlgorithm.Algorithm, algorithm));

                _outer.BaseKeySize = cngKey.KeySize;
            }
        }

        /// <summary>
        /// Note! This can and likely will throw if the algorithm was given a hardware-based key.
        /// </summary>
        public byte[] GetKeyIfExportable()
        {
            if (KeyInPlainText)
            {
                return _outer.BaseKey;
            }
            else
            {
                using (CngKey cngKey = ProduceCngKey())
                {
                    return cngKey.GetSymmetricKeyDataIfExportable(_algorithm);
                }
            }
        }

        public void SetKey(byte[] key)
        {
            _outer.BaseKey = key;
            _keyName = null; // Setting _keyName to null signifies that this object is now based on a plaintext key, not a stored CNG key.
        }

        public void SetKeySize(int keySize, ICngSymmetricAlgorithm outer)
        {
            // Warning: This gets invoked once before "this" is initialized, due to Aes(), DES(), etc., setting the KeySize property in their
            // nullary constructor. That's why we require "outer" being passed as parameter.
            Debug.Assert(_outer == null || _outer == outer);

            outer.BaseKeySize = keySize;
            _keyName = null; // Setting _keyName to null signifies that this object is now based on a plaintext key, not a stored CNG key.
        }

        public void GenerateKey()
        {
            byte[] key = Helpers.GenerateRandom(_outer.BaseKeySize.BitSizeToByteSize());
            SetKey(key);
        }

        public void GenerateIV()
        {
            byte[] iv = Helpers.GenerateRandom(_outer.BlockSize.BitSizeToByteSize());
            _outer.IV = iv;
        }

        public ICryptoTransform CreateEncryptor()
        {
            return CreateCryptoTransform(encrypting: true);
        }

        public ICryptoTransform CreateDecryptor()
        {
            return CreateCryptoTransform(encrypting: false);
        }

        public ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateCryptoTransform(rgbKey, rgbIV, encrypting: true);
        }

        public ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateCryptoTransform(rgbKey, rgbIV, encrypting: false);
        }

        private ICryptoTransform CreateCryptoTransform(bool encrypting)
        {
            return CreateCryptoTransformCore(ProduceCngKey, _outer.IV, encrypting);
        }

        private ICryptoTransform CreateCryptoTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            byte[] key = rgbKey.CloneByteArray();

            long keySize = key.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(_outer.LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (_outer.IsWeakKey(key))
                throw new CryptographicException(SR.Cryptography_WeakKey);

            if (rgbIV != null && rgbIV.Length != _outer.BlockSize.BitSizeToByteSize())
                throw new ArgumentException(SR.Cryptography_InvalidIVSize, nameof(rgbIV));

            if (rgbIV == null && _outer.Mode != CipherMode.ECB)
                throw new CryptographicException(SR.Cryptography_MissingIV);

            string algorithm = _algorithm;
            return CreateCryptoTransformCore(() => key.ToCngKey(algorithm), rgbIV.CloneByteArray(), encrypting);
        }

        private ICryptoTransform CreateCryptoTransformCore(Func<CngKey> cngKeyFactory, byte[] iv, bool encrypting)
        {
            // note: iv is guaranteed to be cloned before this method, so no need to clone it again

            int blockSizeInBytes = _outer.BlockSize.BitSizeToByteSize();
            BasicSymmetricCipher cipher = new BasicSymmetricCipherCng(cngKeyFactory, _outer.Mode, blockSizeInBytes, iv, encrypting);
            return UniversalCryptoTransform.Create(_outer.Padding, cipher, encrypting);
        }

        private CngKey ProduceCngKey()
        {
            if (KeyInPlainText)
            {
                byte[] key = _outer.BaseKey;
                return key.ToCngKey(_algorithm);
            }
            else
            {
                return CngKey.Open(_keyName, _provider, _optionOptions);
            }
        }

        private bool KeyInPlainText
        {
            get { return _keyName == null; }
        }

        private readonly string _algorithm;
        private readonly ICngSymmetricAlgorithm _outer;

        // If using a stored CNG key, these fields provide the CngKey.Open() parameters. If using a plaintext key, _keyName is set to null.
        private string _keyName;
        private CngProvider _provider;
        private CngKeyOpenOptions _optionOptions;

        private const int BitsPerByte = 8;
    }
}

