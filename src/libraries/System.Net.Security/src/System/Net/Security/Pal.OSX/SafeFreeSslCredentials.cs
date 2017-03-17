// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal sealed class SafeFreeSslCredentials : SafeFreeCredentials
    {
        public SafeFreeSslCredentials(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert(
                certificate == null || certificate is X509Certificate2,
                "Only X509Certificate2 certificates are supported at this time");

            X509Certificate2 cert = (X509Certificate2)certificate;

            if (cert != null)
            {
                Debug.Assert(cert.HasPrivateKey, "cert.HasPrivateKey");

                // Make a defensive copy of the certificate. In some async cases the
                // certificate can have been disposed before being provided to the handshake.
                //
                // This meshes with the Unix (OpenSSL) PAL, because it extracts the private key
                // and cert handle (which get up-reffed) to match the API expectations.
                cert = new X509Certificate2(cert);

                Debug.Assert(cert.HasPrivateKey, "cert clone.HasPrivateKey");
            }

            Certificate = cert;
            Protocols = protocols;
            Policy = policy;
        }

        public EncryptionPolicy Policy { get; }

        public SslProtocols Protocols { get; }

        public X509Certificate2 Certificate { get; }

        public override bool IsInvalid => false;

        protected override bool ReleaseHandle()
        {
            Certificate?.Dispose();
            return true;
        }
    }
}
