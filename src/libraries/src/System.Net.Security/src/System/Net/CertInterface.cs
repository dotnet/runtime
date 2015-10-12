// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal abstract class CertInterface
    {
        internal abstract X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateStore);

        internal abstract SslPolicyErrors VerifyCertificateProperties(X509Chain chain, X509Certificate2 certificate, bool checkCertName, bool isServer, string hostName);

        internal abstract string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext);

        internal abstract X509Store EnsureStoreOpened(bool isMachineStore);
    }
}