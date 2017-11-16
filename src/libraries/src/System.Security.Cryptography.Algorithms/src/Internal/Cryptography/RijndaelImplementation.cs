// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    /// <summary>
    /// Internal implementation of Rijndael.
    /// This class is returned from Rijndael.Create() instead of the public RijndaelManaged to 
    /// be consistent with the rest of the static Create() methods which return opaque types.
    /// They both have the same implementation.
    /// </summary>
    internal sealed class RijndaelImplementation : Rijndael
    {
        private readonly Aes _impl;

        internal RijndaelImplementation()
        {
            LegalBlockSizesValue = new KeySizes[] { new KeySizes(minSize: 128, maxSize: 128, skipSize: 0) };

            // This class wraps Aes
            _impl = Aes.Create();
        }

        public override int BlockSize
        {
            get { return _impl.BlockSize; }
            set
            {
                Debug.Assert(BlockSizeValue == 128);

                // Values which were legal in desktop RijndaelManaged but not here in this wrapper type
                if (value == 192 || value == 256)
                    throw new PlatformNotSupportedException(SR.Cryptography_Rijndael_BlockSize);

                // Any other invalid block size will get the normal "invalid block size" exception.
                if (value != 128)
                    throw new CryptographicException(SR.Cryptography_Rijndael_BlockSize);
            }
        }

        public override byte[] IV
        {
            get { return _impl.IV; }
            set { _impl.IV = value; }
        }

        public override byte[] Key
        {
            get { return _impl.Key; }
            set { _impl.Key = value; }
        }

        public override int KeySize
        {
            get { return _impl.KeySize; }
            set { _impl.KeySize = value; }
        }
        public override CipherMode Mode
        {
            get { return _impl.Mode; }
            set { _impl.Mode = value; }
        }

        public override PaddingMode Padding
        {
            get { return _impl.Padding; }
            set { _impl.Padding = value; }
        }

        public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;
        public override ICryptoTransform CreateEncryptor() => _impl.CreateEncryptor();
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) => _impl.CreateEncryptor(rgbKey, rgbIV);
        public override ICryptoTransform CreateDecryptor() => _impl.CreateDecryptor();
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) => _impl.CreateDecryptor(rgbKey, rgbIV);
        public override void GenerateIV() => _impl.GenerateIV();
        public override void GenerateKey() => _impl.GenerateKey();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
            }
        }
    }
}
