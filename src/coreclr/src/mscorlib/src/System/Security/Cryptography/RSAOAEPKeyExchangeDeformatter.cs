// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter {
        private RSA _rsaKey; // RSA Key value to do decrypt operation

        //
        // public constructors
        //

        public RSAOAEPKeyExchangeDeformatter() {}
        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        //
        // public properties
        //

        public override String Parameters {
            get { return null; }
            set { ; }
        }

        //
        // public methods
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override byte[] DecryptKeyExchange(byte[] rgbData) {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            if (_rsaKey is RSACryptoServiceProvider) {
                return ((RSACryptoServiceProvider) _rsaKey).Decrypt(rgbData, true);
            } else {
                return Utils.RsaOaepDecrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), rgbData);
            }
        }

        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }
    }
}
