// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class RSA : AsymmetricAlgorithm
    {
        //Extending this class allows us to know that you are really implementing
        //an RSA key.  This is required for anybody providing a new RSA key value implemention.
        //  The class provides no methods, fields or anything else.  Its only purpose is
        //  as a heirarchy member for identification of algorithm.
        protected RSA() { }

        // Apply the private key to the data.  This function represents a
        // raw RSA operation -- no implicit depadding of the imput value
        public abstract byte[] DecryptValue(byte[] rgb);

        // Apply the public key to the data.  Again, this is a raw operation, no
        // automatic padding.
        public abstract byte[] EncryptValue(byte[] rgb);
        public abstract RSAParameters ExportParameters(bool includePrivateParameters);
        public abstract void ImportParameters(RSAParameters parameters);

        //Implementing this in the derived class. This was implemented in AsymmetricAlgorithm
        // earlier. Now it is expected that class deriving from AsymmetricAlgorithm will implement this
        protected KeySizes[] _legalKeySizesValue;
        public override KeySizes[] LegalKeySizes
        {
            get
            {
                if (null != _legalKeySizesValue)
                {
                    return (KeySizes[])_legalKeySizesValue.Clone();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
