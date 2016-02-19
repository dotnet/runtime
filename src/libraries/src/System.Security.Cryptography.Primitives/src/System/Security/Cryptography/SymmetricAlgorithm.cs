// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class SymmetricAlgorithm : IDisposable
    {
        protected SymmetricAlgorithm()
        {
            Mode = CipherMode.CBC;
            Padding = PaddingMode.PKCS7;
        }

        public virtual int BlockSize
        {
            get
            {
                return _blockSize;
            }

            set
            {
                bool validatedByZeroSkipSizeKeySizes;
                if (!value.IsLegalSize(this.LegalBlockSizes, out validatedByZeroSkipSizeKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidBlockSize);

                if (_blockSize == value && !validatedByZeroSkipSizeKeySizes) // The !validatedByZeroSkipSizeKeySizes check preserves a very obscure back-compat behavior.
                    return;

                _blockSize = value;
                _iv = null;
                return;
            }
        }

        public virtual byte[] IV
        {
            get
            {
                if (_iv == null)
                    GenerateIV();
                return _iv.CloneByteArray();
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length != this.BlockSize / 8)
                    throw new CryptographicException(SR.Cryptography_InvalidIVSize);

                _iv = value.CloneByteArray();
            }
        }

        public virtual byte[] Key
        {
            get
            {
                if (_key == null)
                    GenerateKey();
                return _key.CloneByteArray();
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                long bitLength = value.Length * 8L;
                if (bitLength > int.MaxValue || !ValidKeySize((int)bitLength))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                // must convert bytes to bits
                this.KeySize = (int)bitLength;
                _key = value.CloneByteArray();
            }
        }

        public virtual int KeySize
        {
            get
            {
                return _keySize;
            }

            set
            {
                if (!ValidKeySize(value))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                _keySize = value;
                _key = null;
            }
        }

        public virtual KeySizes[] LegalBlockSizes
        {
            get
            {
                // Desktop compat: Unless derived classes set the protected field "LegalBlockSizesValue" to a non-null value, a NullReferenceException is what you get.
                // In the Win8P profile, the "LegalBlockSizesValue" field has been removed. So derived classes must override this property for the class to be any of any use.
                throw new NullReferenceException();
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // Desktop compat: Unless derived classes set the protected field "LegalKeySizesValue" to a non-null value, a NullReferenceException is what you get.
                // In the Win8P profile, the "LegalKeySizesValue" field has been removed. So derived classes must override this property for the class to be any of any use.
                throw new NullReferenceException();
            }
        }

        public virtual CipherMode Mode
        {
            get
            {
                return _cipherMode;
            }

            set
            {
                if (!(value == CipherMode.CBC || value == CipherMode.ECB))
                    throw new CryptographicException(SR.Cryptography_InvalidCipherMode);

                _cipherMode = value;
            }
        }

        public virtual PaddingMode Padding
        {
            get
            {
                return _paddingMode;
            }

            set
            {
                if (!(value == PaddingMode.None || value == PaddingMode.PKCS7 || value == PaddingMode.Zeros))
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
                _paddingMode = value;
            }
        }

        public virtual ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(this.Key, this.IV);
        }

        public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);

        public virtual ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(Key, IV);
        }

        public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_key != null)
                {
                    Array.Clear(_key, 0, _key.Length);
                    _key = null;
                }
                if (_iv != null)
                {
                    Array.Clear(_iv, 0, _iv.Length);
                    _iv = null;
                }
            }
        }

        public abstract void GenerateIV();

        public abstract void GenerateKey();

        private bool ValidKeySize(int bitLength)
        {
            KeySizes[] validSizes = this.LegalKeySizes;
            if (validSizes == null)
                return false;
            return bitLength.IsLegalSize(validSizes);
        }


        private CipherMode _cipherMode;
        private PaddingMode _paddingMode;
        private byte[] _key;
        private byte[] _iv;
        private int _blockSize;
        private int _keySize;
    }
}
