// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public class RSAOAEPKeyExchangeFormatter : AsymmetricKeyExchangeFormatter
    {
        private byte[] ParameterValue;
        private RSA _rsaKey;
        private RandomNumberGenerator RngValue;

        public RSAOAEPKeyExchangeFormatter() { }
        public RSAOAEPKeyExchangeFormatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _rsaKey = (RSA)key;
        }

        public byte[] Parameter
        {
            get
            {
                if (ParameterValue != null)
                {
                    return (byte[])ParameterValue.Clone();
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    ParameterValue = (byte[])value.Clone();
                }
                else
                {
                    ParameterValue = null;
                }
            }
        }

        public override string Parameters
        {
            get {return null;}
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

            return _rsaKey.Encrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
        }
    }
}
