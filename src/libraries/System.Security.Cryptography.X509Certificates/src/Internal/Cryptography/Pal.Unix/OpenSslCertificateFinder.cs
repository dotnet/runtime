// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslCertificateFinder : ManagedCertificateFinder
    {
        internal OpenSslCertificateFinder(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
            : base(findFrom, copyTo, validOnly)
        {
        }

        protected override byte[] GetSubjectPublicKeyInfo(X509Certificate2 cert)
        {
            OpenSslX509CertificateReader certPal = (OpenSslX509CertificateReader)cert.Pal;

            byte[] publicKeyInfoBytes = Interop.Crypto.OpenSslEncode(
                Interop.Crypto.GetX509SubjectPublicKeyInfoDerSize,
                Interop.Crypto.EncodeX509SubjectPublicKeyInfo,
                certPal.SafeHandle);

            return publicKeyInfoBytes;
        }
        
        protected override X509Certificate2 CloneCertificate(X509Certificate2 cert)
        {
            OpenSslX509CertificateReader certPal = (OpenSslX509CertificateReader)cert.Pal;
            return new X509Certificate2(certPal.DuplicateHandles());
        }
    }
}
