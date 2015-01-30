// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1SignatureDeformatter : AsymmetricSignatureDeformatter {
        //
        //  This class provides the PKCS#1 v1.5 signature format processing during
        //  the verification process (i.e. decrypting the object).  The class has
        //  some special code for dealing with the CSP based RSA keys as the 
        //  formatting and verification is done within the CSP rather than in
        //  managed code.
        //

        private RSA    _rsaKey; // RSA Key value to do decrypt operation
        private String _strOID; // OID value for the HASH algorithm

        //
        // public constructors
        //

        public RSAPKCS1SignatureDeformatter() {}
        public RSAPKCS1SignatureDeformatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
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

        public override void SetHashAlgorithm(String strName) {
            _strOID = CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");
            Contract.EndContractBlock();

            if (_strOID == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            // Two cases here -- if we are talking to the CSP version or if we are talking to some other RSA provider.
            if (_rsaKey is RSACryptoServiceProvider) {
                int calgHash = X509Utils.GetAlgIdFromOid(_strOID, OidGroup.HashAlgorithm);
                return ((RSACryptoServiceProvider)_rsaKey).VerifyHash(rgbHash, calgHash, rgbSignature);
            }
            else {
                byte[] pad = Utils.RsaPkcs1Padding(_rsaKey, CryptoConfig.EncodeOID(_strOID), rgbHash);
                // Apply the public key to the signature data to get back the padded buffer actually signed.
                // Compare the two buffers to see if they match; ignoring any leading zeros
                return Utils.CompareBigIntArrays(_rsaKey.EncryptValue(rgbSignature), pad);
            }
        }
    }
}
