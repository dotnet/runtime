// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public class RSAPKCS1KeyExchangeFormatter : AsymmetricKeyExchangeFormatter
    {
        private RSA _rsaKey;
        private RandomNumberGenerator RngValue;

        public RSAPKCS1KeyExchangeFormatter() { }

        public RSAPKCS1KeyExchangeFormatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _rsaKey = (RSA)key;
        }

        public override string Parameters
        {
            get
            {
                return "<enc:KeyEncryptionMethod enc:Algorithm=\"http://www.microsoft.com/xml/security/algorithm/PKCS1-v1.5-KeyEx\" xmlns:enc=\"http://www.microsoft.com/xml/security/encryption/v1.0\" />";
            }
        }

        public RandomNumberGenerator Rng {
            get { return RngValue; }
            set { RngValue = value; }
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _rsaKey = (RSA)key;
        }

        public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType)
        {
            return CreateKeyExchange(rgbData);
        }

        public override byte[] CreateKeyExchange(byte[] rgbData)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_MissingKey);

            return _rsaKey.Encrypt(rgbData, RSAEncryptionPadding.Pkcs1);
        }
    }
}
