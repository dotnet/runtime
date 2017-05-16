// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Wrapper for public key material passed between parties during Diffie-Hellman key material generation
    /// </summary>
    public abstract class ECDiffieHellmanPublicKey : IDisposable
    {
        private readonly byte[] _keyBlob;

        protected ECDiffieHellmanPublicKey(byte[] keyBlob)
        {
            if (keyBlob == null)
            {
                throw new ArgumentNullException(nameof(keyBlob));
            }

            _keyBlob = keyBlob.Clone() as byte[];
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        public virtual byte[] ToByteArray()
        {
            return _keyBlob.Clone() as byte[];
        }

        // This method must be implemented by derived classes. In order to conform to the contract, it cannot be abstract.
        public virtual string ToXmlString()
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }
    }
}
