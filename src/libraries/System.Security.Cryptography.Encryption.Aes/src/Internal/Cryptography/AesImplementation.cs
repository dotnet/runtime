// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed partial class AesImplementation : Aes
    {
        public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV, encrypting: false);
        }

        public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV, encrypting: true);
        }

        public sealed override void GenerateIV()
        {
            byte[] iv = new byte[this.BlockSize / 8];
            s_rng.GetBytes(iv);
            this.IV = iv;
        }

        public sealed override void GenerateKey()
        {
            byte[] key = new byte[this.KeySize / 8];
            s_rng.GetBytes(key);
            this.Key = key;
        }

        protected sealed override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("key");
            int keySize = rgbKey.Length * 8;
            if (!keySize.IsLegalSize(this.LegalKeySizes))
                throw new ArgumentException(SR.Format(SR.Cryptography_InvalidKeySize, "key"));
            if (rgbIV != null && rgbIV.Length * 8 != this.BlockSize)
                throw new ArgumentException(SR.Format(SR.Cryptography_InvalidIVSize, "iv"));

            if (encrypting)
                return CreateEncryptor(Mode, Padding, rgbKey, rgbIV, BlockSize / 8);
            else
                return CreateDecryptor(Mode, Padding, rgbKey, rgbIV, BlockSize / 8);
        }

        private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
    }
}
