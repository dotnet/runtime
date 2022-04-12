// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Wrapper for public key material passed between parties during Diffie-Hellman key material generation
    /// </summary>
    public abstract partial class ECDiffieHellmanPublicKey : IDisposable
    {
        private readonly byte[] _keyBlob;

        protected ECDiffieHellmanPublicKey()
        {
            _keyBlob = Array.Empty<byte>();
        }

        [Obsolete(Obsoletions.EcDhPublicKeyBlobMessage, DiagnosticId = Obsoletions.EcDhPublicKeyBlobDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        protected ECDiffieHellmanPublicKey(byte[] keyBlob!!)
        {
            _keyBlob = (byte[])keyBlob.Clone();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        [Obsolete(Obsoletions.EcDhPublicKeyBlobMessage, DiagnosticId = Obsoletions.EcDhPublicKeyBlobDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual byte[] ToByteArray()
        {
            return (byte[])_keyBlob.Clone();
        }

        // This method must be implemented by derived classes. In order to conform to the contract, it cannot be abstract.
        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual string ToXmlString()
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }
    }
}
