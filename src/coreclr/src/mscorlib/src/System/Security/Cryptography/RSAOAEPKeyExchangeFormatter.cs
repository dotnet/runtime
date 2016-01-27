// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAOAEPKeyExchangeFormatter : AsymmetricKeyExchangeFormatter {
        private byte[] ParameterValue;
        private RSA _rsaKey;
        private RandomNumberGenerator RngValue;

        //
        // public constructors
        //

        public RSAOAEPKeyExchangeFormatter() {}
        public RSAOAEPKeyExchangeFormatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        //
        // public properties
        //

        /// <internalonly/>
        public byte[] Parameter {
            get {
                if (ParameterValue != null)
                    return (byte[]) ParameterValue.Clone(); 
                return null;
            }
            set {
                if (value != null)
                    ParameterValue = (byte[]) value.Clone();
                else 
                    ParameterValue = null;
            }
        }

        /// <internalonly/>
        public override String Parameters {
            get { return null; }
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override byte[] CreateKeyExchange(byte[] rgbData) {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            if (_rsaKey is RSACryptoServiceProvider) {
                return ((RSACryptoServiceProvider) _rsaKey).Encrypt(rgbData, true);
            } else {
                return Utils.RsaOaepEncrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), RandomNumberGenerator.Create(), rgbData);
            }
        }

        public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType) {
            return CreateKeyExchange(rgbData);
        }
    }
}
