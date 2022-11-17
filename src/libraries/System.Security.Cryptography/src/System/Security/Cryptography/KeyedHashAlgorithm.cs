// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class KeyedHashAlgorithm : HashAlgorithm
    {
        protected KeyedHashAlgorithm() { }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static new KeyedHashAlgorithm Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfigForwarder.CreateFromNameUnreferencedCodeMessage)]
        public static new KeyedHashAlgorithm? Create(string algName) =>
            CryptoConfigForwarder.CreateFromName<KeyedHashAlgorithm>(algName);

        public virtual byte[] Key
        {
            get
            {
                return KeyValue.CloneByteArray();
            }

            set
            {
                KeyValue = value.CloneByteArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // For keyed hash algorithms, we always want to zero out the key value
            if (disposing)
            {
                if (KeyValue != null)
                {
                    Array.Clear(KeyValue);
                }
                KeyValue = null!;
            }
            base.Dispose(disposing);
        }

        protected byte[] KeyValue = null!;
    }
}
