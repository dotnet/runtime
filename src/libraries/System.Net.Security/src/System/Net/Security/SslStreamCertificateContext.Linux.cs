// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        // No leaf, no root.
        private const int _trimChain = 2;

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) => Create(target, null);
}
