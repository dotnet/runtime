// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class DSASignatureFormatter : AsymmetricSignatureFormatter
    {
        private DSA _dsaKey;

        public DSASignatureFormatter() { }

        public DSASignatureFormatter(AsymmetricAlgorithm key) : this()
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _dsaKey = (DSA)key;
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

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
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_MissingKey);

            return _dsaKey.CreateSignature(rgbHash);
        }
    }
}
