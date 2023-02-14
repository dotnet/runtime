// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        // No leaf, no root.
        private const bool TrimRootCertificate = true;

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates, SslCertificateTrust? trust)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
            Trust = trust;
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) => Create(target, null, offline: false, trust: null, noOcspFetch: true);

#pragma warning disable CA1822 //does not access instance data and can be marked as static. Other partials do access instance data.
        private partial void AddRootCertificate(X509Certificate2? rootCertificate)
        {
            // macOS doesn't need the root. Dispose of it, if we have one.
            rootCertificate?.Dispose();
        }
#pragma warning restore CA1822
    }
}
