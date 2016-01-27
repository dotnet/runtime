// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter {
        RSA _rsaKey;
        RandomNumberGenerator RngValue;

        // Constructors

        public RSAPKCS1KeyExchangeDeformatter() {}

        public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        //
        // public properties
        //

        public RandomNumberGenerator RNG {
            get { return RngValue; }
            set { RngValue = value; }
        }
        
        public override String Parameters {
            get { return null; }
            set { ;}
        }

        //
        // public methods
        //

        public override byte[] DecryptKeyExchange(byte[] rgbIn) {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            byte[] rgbOut;
            if (_rsaKey is RSACryptoServiceProvider) {
                rgbOut = ((RSACryptoServiceProvider) _rsaKey).Decrypt(rgbIn, false);
            }
            else {
                int i;
                byte[] rgb;
                rgb = _rsaKey.DecryptValue(rgbIn);

                //
                //  Expected format is:
                //      00 || 02 || PS || 00 || D
                //      where PS does not contain any zeros.
                //

                for (i = 2; i<rgb.Length; i++) {
                    if (rgb[i] == 0) {
                        break;
                    }
                }

                if (i >= rgb.Length)
                    throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_PKCS1Decoding"));

                i++;            // Skip over the zero

                rgbOut = new byte[rgb.Length - i];
                Buffer.InternalBlockCopy(rgb, i, rgbOut, 0, rgbOut.Length);
            }
            return rgbOut;
        }

        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }
    }
}
