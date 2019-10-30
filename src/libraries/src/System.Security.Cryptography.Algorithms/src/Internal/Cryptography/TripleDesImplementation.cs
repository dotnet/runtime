// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350")]
    internal sealed partial class TripleDesImplementation : TripleDES
    {
        private const int BitsPerByte = 8;

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
        }

        public override void GenerateIV()
        {
            byte[] iv = new byte[BlockSize / BitsPerByte];
            RandomNumberGenerator.Fill(iv);
            IV = iv;
        }

        public sealed override void GenerateKey()
        {
            byte[] key = new byte[KeySize / BitsPerByte];
            RandomNumberGenerator.Fill(key);
            Key = key;
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (rgbIV != null)
            {
                long ivSize = rgbIV.Length * (long)BitsPerByte;
                if (ivSize != BlockSize)
                    throw new ArgumentException(SR.Cryptography_InvalidIVSize, nameof(rgbIV));
            }

            if (rgbKey.Length == 16)
            {
                // Some platforms do not support Two-Key Triple DES, so manually support it here.
                // Two-Key Triple DES contains two 8-byte keys {K1}{K2} with {K1} appended to make {K1}{K2}{K1}.
                byte[] newkey = new byte[24];
                Array.Copy(rgbKey, 0, newkey, 0, 16);
                Array.Copy(rgbKey, 0, newkey, 16, 8);
                rgbKey = newkey;
            }

            return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / BitsPerByte, encrypting);
        }
    }
}
