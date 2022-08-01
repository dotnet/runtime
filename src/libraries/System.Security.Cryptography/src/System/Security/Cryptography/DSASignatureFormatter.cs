// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public class DSASignatureFormatter : AsymmetricSignatureFormatter
    {
        private DSA? _dsaKey;

        public DSASignatureFormatter() { }

        public DSASignatureFormatter(AsymmetricAlgorithm key) : this()
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

        public override byte[] CreateSignature(byte[] rgbHash)
        {
            ArgumentNullException.ThrowIfNull(rgbHash);

            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_FormatterMissingKey);

            return _dsaKey.CreateSignature(rgbHash);
        }
    }
}
