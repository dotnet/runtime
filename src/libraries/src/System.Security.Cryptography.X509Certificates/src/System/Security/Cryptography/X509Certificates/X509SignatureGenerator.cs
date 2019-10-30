// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates
{
    public abstract class X509SignatureGenerator
    {
        private PublicKey _publicKey;

        public PublicKey PublicKey
        {
            get
            {
                if (_publicKey == null)
                {
                    _publicKey = BuildPublicKey();
                }

                return _publicKey;
            }
        }

        public abstract byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm);
        public abstract byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm);
        protected abstract PublicKey BuildPublicKey();

        public static X509SignatureGenerator CreateForECDsa(ECDsa key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return new ECDsaX509SignatureGenerator(key);
        }

        public static X509SignatureGenerator CreateForRSA(RSA key, RSASignaturePadding signaturePadding)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (signaturePadding == null)
                throw new ArgumentNullException(nameof(signaturePadding));

            if (signaturePadding == RSASignaturePadding.Pkcs1)
                return new RSAPkcs1X509SignatureGenerator(key);
            if (signaturePadding.Mode == RSASignaturePaddingMode.Pss)
                return new RSAPssX509SignatureGenerator(key, signaturePadding);

            throw new ArgumentException(SR.Cryptography_InvalidPaddingMode);
        }
    }
}
