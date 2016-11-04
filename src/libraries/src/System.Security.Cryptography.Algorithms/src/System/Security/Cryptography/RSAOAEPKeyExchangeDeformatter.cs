// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
    {
        private RSA _rsaKey;

        public RSAOAEPKeyExchangeDeformatter() { }
        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _rsaKey = (RSA)key;
        }

        public override string Parameters
        {
            get {return null;}
            set { }
        }

        public override byte[] DecryptKeyExchange(byte[] rgbData)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_MissingKey);

            return _rsaKey.Decrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _rsaKey = (RSA)key;
        }
    }
}
