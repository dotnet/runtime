// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal static class CertWrapper
    {
        static CertInterface certModule = new CertModule();

        internal static SslPolicyErrors VerifyRemoteCertName(X509Chain chain, bool isServer, string hostName)
        {
            return certModule.VerifyRemoteCertName(chain, isServer, hostName);
        }

        internal static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateStore)
        {
            return certModule.GetRemoteCertificate(securityContext, out remoteCertificateStore);
        }

        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            return certModule.GetRequestCertificateAuthorities(securityContext);
        }

    }
}
