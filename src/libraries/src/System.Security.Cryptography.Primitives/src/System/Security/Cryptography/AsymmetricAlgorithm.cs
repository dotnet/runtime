// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        protected KeySizes[] LegalKeySizesValue;

        protected AsymmetricAlgorithm()
        {
        }

        public virtual int KeySize
        {
            get
            {
                return KeySizeValue;
            }

            set
            {
                if (!value.IsLegalSize(this.LegalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                KeySizeValue = value;
                return;
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // Desktop compat: No null check is performed
                return (KeySizes[])LegalKeySizesValue.Clone();
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
    }
}
