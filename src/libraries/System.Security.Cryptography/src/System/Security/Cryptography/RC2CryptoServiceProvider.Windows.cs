// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RC2CryptoServiceProvider : RC2
    {
        private bool _use40bitSalt;
        private const int BitsPerByte = 8;

        private static readonly KeySizes[] s_legalKeySizes =
        {
            new KeySizes(40, 128, 8)  // csp implementation only goes up to 128
        };

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public RC2CryptoServiceProvider()
        {
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
            FeedbackSizeValue = 8;
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
            [SupportedOSPlatform("windows")]
            set
            {
                _use40bitSalt = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of RC2")]
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of RC2")]
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV?.CloneByteArray(), encrypting: false);
        }

        public override void GenerateKey()
        {
            KeyValue = RandomNumberGenerator.GetBytes(KeySizeValue / 8);
        }

        public override void GenerateIV()
        {
            // Block size is always 64 bits so IV is always 64 bits == 8 bytes
            IVValue = RandomNumberGenerator.GetBytes(8);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of RC2")]
        private UniversalCryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(this.LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (rgbIV == null)
            {
                if (Mode.UsesIv())
                {
                    rgbIV = RandomNumberGenerator.GetBytes(8);
                }
            }
            else
            {
                // We truncate IV's that are longer than the block size to 8 bytes : this is
                // done to maintain backward .NET Framework compatibility with the behavior shipped in V1.x.
                // The call to set the IV in CryptoAPI will ignore any bytes after the first 8
                // bytes. We'll still reject IV's that are shorter than the block size though.
                if (rgbIV.Length < 8)
                    throw new CryptographicException(SR.Cryptography_InvalidIVSize);
            }

            Debug.Assert(EffectiveKeySize == KeySize);
            BasicSymmetricCipher cipher = new BasicSymmetricCipherCsp(CapiHelper.CALG_RC2, Mode, BlockSize / BitsPerByte, rgbKey, !UseSalt, rgbIV, encrypting, 0, 0);
            return UniversalCryptoTransform.Create(Padding, cipher, encrypting);
        }
    }
}
