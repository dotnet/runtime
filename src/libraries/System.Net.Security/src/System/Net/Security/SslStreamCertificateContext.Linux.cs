// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        private const bool TrimRootCertificate = true;
        internal readonly ConcurrentDictionary<SslProtocols, SafeSslContextHandle> SslContexts;

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates, SslCertificateTrust? trust)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
            Trust = trust;
            SslContexts = new ConcurrentDictionary<SslProtocols, SafeSslContextHandle>();
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) => Create(target, null);
    }
}
