// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1KeyExchangeFormatter : AsymmetricKeyExchangeFormatter {
        RandomNumberGenerator RngValue;
        RSA _rsaKey;

        //
        // public constructors
        //

        public RSAPKCS1KeyExchangeFormatter() {}

        public RSAPKCS1KeyExchangeFormatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        //
        // public properties
        //

        public override String Parameters {
            get { return "<enc:KeyEncryptionMethod enc:Algorithm=\"http://www.microsoft.com/xml/security/algorithm/PKCS1-v1.5-KeyEx\" xmlns:enc=\"http://www.microsoft.com/xml/security/encryption/v1.0\" />"; }
        }

        public RandomNumberGenerator Rng {
            get { return RngValue; }
            set { RngValue = value; }
        }
        
        //
        // public methods
        //

        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        public override byte[] CreateKeyExchange(byte[] rgbData) {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            byte[] rgbKeyEx;
            if (_rsaKey is RSACryptoServiceProvider) {
                rgbKeyEx = ((RSACryptoServiceProvider) _rsaKey).Encrypt(rgbData, false);
            }
            else {
                int cb = _rsaKey.KeySize/8;
                if ((rgbData.Length + 11) > cb)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_EncDataTooBig", cb-11));
                byte[]  rgbInput = new byte[cb];

                //
                //  We want to pad to the following format:
                //      00 || 02 || PS || 00 || D
                //
                //      PS - pseudorandom non zero bytes
                //      D - data
                //

                if (RngValue == null) {
                    RngValue = RandomNumberGenerator.Create();
                }
                
                Rng.GetNonZeroBytes(rgbInput);
                rgbInput[0] = 0;
                rgbInput[1] = 2;
                rgbInput[cb-rgbData.Length-1] = 0;
                Buffer.InternalBlockCopy(rgbData, 0, rgbInput, cb-rgbData.Length, rgbData.Length);

                //
                //  Now encrypt the value and return it. (apply public key)
                //

                rgbKeyEx = _rsaKey.EncryptValue(rgbInput);
            }
            return rgbKeyEx;
        }

        public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType) {
            return CreateKeyExchange(rgbData);
        }
    }
}
