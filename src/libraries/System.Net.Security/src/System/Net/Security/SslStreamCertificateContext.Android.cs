// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        private const bool TrimRootCertificate = true;

        private SslStreamCertificateContext(X509Certificate2 target, ReadOnlyCollection<X509Certificate2> intermediates, SslCertificateTrust? trust)
        {
            IntermediateCertificates = intermediates;
            TargetCertificate = target;
            Trust = trust;
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) => Create(target, null);
    }
}
