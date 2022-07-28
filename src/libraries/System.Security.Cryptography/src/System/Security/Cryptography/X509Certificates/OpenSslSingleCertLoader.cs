// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslSingleCertLoader : ILoaderPal
    {
        private ICertificatePal? _cert;

        public OpenSslSingleCertLoader(ICertificatePal cert)
        {
            _cert = cert;
        }

        public void Dispose()
        {
            // If there's still a certificate, dispose it.
            _cert?.Dispose();
        }

        public void MoveTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            ICertificatePal? localCert = Interlocked.Exchange(ref _cert, null);
            Debug.Assert(localCert != null);

            collection.Add(new X509Certificate2(localCert));
        }
    }
}
