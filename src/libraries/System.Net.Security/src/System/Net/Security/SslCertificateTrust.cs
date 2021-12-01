// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public sealed class SslCertificateTrust
    {
        internal X509Store? _store;
        internal X509Certificate2Collection? _trustList;
        internal bool _sendTrustInHandshake;

        public static SslCertificateTrust CreateForX509Store(X509Store store, bool sendTrustInHandshake = false)
        {

#if TARGET_WINDOWS
            if (sendTrustInHandshake && store.Location != StoreLocation.LocalMachine)
            {
                throw new PlatformNotSupportedException(SR.net_ssl_trust_store);
            }
#else
            if (sendTrustInHandshake)
            {
                // to be removed when implemented.
                throw new PlatformNotSupportedException("Not supported yet.");
            }
#endif
            if (!store.IsOpen)
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            }

            var trust = new SslCertificateTrust();
            trust._store = store;
            trust._sendTrustInHandshake = sendTrustInHandshake;
            return trust;
        }

        [UnsupportedOSPlatform("windows")]
        public static SslCertificateTrust CreateForX509Collection(X509Certificate2Collection trustList, bool sendTrustInHandshake = false)
        {
            if (sendTrustInHandshake)
            {
                // to be removed when implemented.
                throw new PlatformNotSupportedException("Not supported yet.");
            }

#if TARGET_WINDOWS
            if (sendTrustInHandshake)
            {
                throw new PlatformNotSupportedException(SR.net_ssl_trust_collection);
            }
#endif
            var trust = new SslCertificateTrust();
            trust._trustList = trustList;
            trust._sendTrustInHandshake = sendTrustInHandshake;
            return trust;
        }

        private SslCertificateTrust() { }
    }
}
