// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.NativeCrypto;
using System.ComponentModel;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RC2CryptoServiceProvider : RC2 
    {
        private bool _use40bitSalt = false;
        private const int BitsPerByte = 8;
        private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();

        private static KeySizes[] s_legalKeySizes =
        {
            new KeySizes(40, 128, 8)  // csp implementation only goes up to 128
        };

        public RC2CryptoServiceProvider()
        {
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
        }

        public override int EffectiveKeySize
        {
            get
            {
                return KeySizeValue;
            }
            set
            {
                if (value != KeySizeValue)
                    throw new CryptographicUnexpectedOperationException(SR.Cryptography_RC2_EKSKS2);
            }
        }

        public bool UseSalt
        {
            get
            {
                return _use40bitSalt;
            }
            set
            {
                _use40bitSalt = value;
            }
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV == null ? null : rgbIV.CloneByteArray(), encrypting: true);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV == null ? null : rgbIV.CloneByteArray(), encrypting: false);
        }

        public override void GenerateKey()
        {
            var key = new byte[KeySizeValue / 8];
            s_rng.GetBytes(key);
            KeyValue = key;
        }

        public override void GenerateIV()
        {
            // Block size is always 64 bits so IV is always 64 bits == 8 bytes
            var iv = new byte[8];
            s_rng.GetBytes(iv);
            IVValue = iv;
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(this.LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (rgbIV == null)
            {
                if (Mode.UsesIv())
                {
                    rgbIV = new byte[8];
                    s_rng.GetBytes(rgbIV);
                }
            }
            else
            {
                // We truncate IV's that are longer than the block size to 8 bytes : this is
                // done to maintain backward desktop compatibility with the behavior shipped in V1.x.
                // The call to set the IV in CryptoAPI will ignore any bytes after the first 8
                // bytes. We'll still reject IV's that are shorter than the block size though.
                if (rgbIV.Length < 8)
                    throw new CryptographicException(SR.Cryptography_InvalidIVSize);
            }

            int effectiveKeySize = EffectiveKeySizeValue == 0 ? (int)keySize : EffectiveKeySize;
            BasicSymmetricCipher cipher = new BasicSymmetricCipherCsp(CapiHelper.CALG_RC2, Mode, BlockSize / BitsPerByte, rgbKey, effectiveKeySize, !UseSalt, rgbIV, encrypting);
            return UniversalCryptoTransform.Create(Padding, cipher, encrypting);
        }
    }
}
