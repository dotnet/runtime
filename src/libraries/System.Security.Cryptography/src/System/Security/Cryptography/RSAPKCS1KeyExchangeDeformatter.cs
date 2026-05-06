// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
    {
        private RSA? _rsaKey;
        private RandomNumberGenerator? RngValue;

        public RSAPKCS1KeyExchangeDeformatter() { }

        public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _rsaKey = (RSA)key;
        }

        public RandomNumberGenerator? RNG
        {
            get { return RngValue; }
            set { RngValue = value; }
        }

        public override string? Parameters
        {
            get { return null; }
            set { }
        }

        public override byte[] DecryptKeyExchange(byte[] rgbIn)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_FormatterMissingKey);

            return _rsaKey.Decrypt(rgbIn, RSAEncryptionPadding.Pkcs1);
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _rsaKey = (RSA)key;
        }
    }
}
