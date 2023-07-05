// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public class DSASignatureDeformatter : AsymmetricSignatureDeformatter
    {
        private DSA? _dsaKey;

        public DSASignatureDeformatter() { }

        public DSASignatureDeformatter(AsymmetricAlgorithm key) : this()
        {
            ArgumentNullException.ThrowIfNull(key);

            _dsaKey = (DSA)key;
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            ArgumentNullException.ThrowIfNull(key);

            _dsaKey = (DSA)key;
        }

        public override void SetHashAlgorithm(string strName)
        {
            if (strName.ToUpperInvariant() != HashAlgorithmNames.SHA1)
            {
                // To match desktop, throw here
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_InvalidOperation);
            }
        }

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
        {
            ArgumentNullException.ThrowIfNull(rgbHash);
            ArgumentNullException.ThrowIfNull(rgbSignature);

            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_FormatterMissingKey);

            return _dsaKey.VerifySignature(rgbHash, rgbSignature);
        }
    }
}
