// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        public sealed override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public sealed override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
        }

        public sealed override void GenerateIV()
        {
            byte[] iv = new byte[BlockSize / BitsPerByte];
            s_rng.GetBytes(iv);
            IV = iv;
        }

        public sealed override void GenerateKey()
        {
            byte[] key = new byte[KeySize / BitsPerByte];
            s_rng.GetBytes(key);
            Key = key;
        }

        protected sealed override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            // note: rbgIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(this.LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, "rgbKey");

            if (rgbIV != null)
            {
                long ivSize = rgbIV.Length * (long)BitsPerByte;
                if (ivSize != BlockSize)
                    throw new ArgumentException(SR.Cryptography_InvalidIVSize, "rgbIV");
            }

            if (encrypting)
                return CreateEncryptor(Mode, Padding, rgbKey, rgbIV, BlockSize / BitsPerByte);
            else
                return CreateDecryptor(Mode, Padding, rgbKey, rgbIV, BlockSize / BitsPerByte);
        }

        private const int BitsPerByte = 8;
        private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
    }
}
