// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        protected KeySizes[] LegalKeySizesValue;

        protected AsymmetricAlgorithm() { }

        public static AsymmetricAlgorithm Create()
        {
            return Create("System.Security.Cryptography.AsymmetricAlgorithm");
        }

        public static AsymmetricAlgorithm Create(string algName)
        {
            throw new PlatformNotSupportedException();
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

        public virtual string SignatureAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string KeyExchangeAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual void FromXmlString(string xmlString)
        {
            throw new NotImplementedException();
        }

        public virtual string ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }
    }
}
