// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected AsymmetricAlgorithm()
        {
        }

        public virtual int KeySize
        {
            get
            {
                return _keySize;
            }

            set
            {
                if (!value.IsLegalSize(this.LegalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                _keySize = value;
                return;
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // Desktop compat: Unless derived classes set the protected field "LegalKeySizesValue" to a non-null value, a NullReferenceException is what you get.
                // In the Win8P profile, the "LegalKeySizesValue" field has been removed. So derived classes must override this property for the class to be any of any use.
                throw new NullReferenceException();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }
        private int _keySize;
    }
}
