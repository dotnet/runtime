// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
    {
        private RSA? _rsaKey;

        public RSAOAEPKeyExchangeDeformatter() { }
        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _rsaKey = (RSA)key;
        }

        public override string? Parameters
        {
            get { return null; }
            set { }
        }

        public override byte[] DecryptKeyExchange(byte[] rgbData)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_FormatterMissingKey);

            return _rsaKey.Decrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _rsaKey = (RSA)key;
        }
    }
}
