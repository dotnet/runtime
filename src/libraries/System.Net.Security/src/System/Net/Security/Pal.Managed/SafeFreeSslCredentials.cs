// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Net
{
    internal sealed class SafeFreeSslCredentials : SafeFreeCredentials
    {
        public SafeFreeSslCredentials(SslStreamCertificateContext? certificateContext, SslProtocols protocols, EncryptionPolicy policy)
            : base(IntPtr.Zero, true)
        {
            if (certificateContext != null)
            {
                // Make a defensive copy of the certificate. In some async cases the
                // certificate can have been disposed before being provided to the handshake.
                //
                // This meshes with the Unix (OpenSSL) PAL, because it extracts the private key
                // and cert handle (which get up-reffed) to match the API expectations.
                certificateContext = certificateContext.Duplicate();

                Debug.Assert(certificateContext.Certificate.HasPrivateKey, "cert clone.HasPrivateKey");
            }

            CertificateContext = certificateContext;
            Protocols = protocols;
            Policy = policy;
        }

        public EncryptionPolicy Policy { get; }

        public SslProtocols Protocols { get; }

        public SslStreamCertificateContext? CertificateContext { get; }

        public override bool IsInvalid => false;

        protected override bool ReleaseHandle()
        {
            CertificateContext?.Certificate.Dispose();
            return true;
        }
    }
}
