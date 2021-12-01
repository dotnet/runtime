// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class FindPal
    {
        internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
        {
            return new AndroidCertificateFinder(findFrom, copyTo, validOnly);
        }

        private sealed class AndroidCertificateFinder : ManagedCertificateFinder
        {
            public AndroidCertificateFinder(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
                : base(findFrom, copyTo, validOnly)
            {
            }

            protected override byte[] GetSubjectPublicKeyInfo(X509Certificate2 cert)
            {
                AndroidCertificatePal pal = (AndroidCertificatePal)cert.Pal;
                return pal.SubjectPublicKeyInfo;
            }

            protected override X509Certificate2 CloneCertificate(X509Certificate2 cert)
            {
                return new X509Certificate2(AndroidCertificatePal.FromOtherCert(cert));
            }
        }
    }
}
