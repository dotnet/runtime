// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        private const bool TrimRootCertificate = true;
        internal readonly ConcurrentDictionary<int, SafeSslContextHandle> contexts;

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates, SslCertificateTrust? trust)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
            Trust = trust;
            contexts = new ConcurrentDictionary<int, SafeSslContextHandle>();
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) => Create(target, null);
    }
}
