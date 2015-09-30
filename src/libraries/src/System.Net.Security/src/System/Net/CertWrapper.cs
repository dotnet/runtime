// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal static class CertWrapper
    {
        private static readonly CertInterface s_certModule = new CertModule();

        internal static SslPolicyErrors VerifyCertificateProperties(X509Chain chain, X509Certificate2 certificate, bool checkCertName, bool isServer, string hostName)
        {
            return s_certModule.VerifyCertificateProperties(chain, certificate, checkCertName, isServer, hostName);
        }

        internal static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateStore)
        {
            return s_certModule.GetRemoteCertificate(securityContext, out remoteCertificateStore);
        }

        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            return s_certModule.GetRequestCertificateAuthorities(securityContext);
        }

        internal static X509Store EnsureStoreOpened(bool isMachineStore)
        {
            return s_certModule.EnsureStoreOpened(isMachineStore);
        }
    }
}
