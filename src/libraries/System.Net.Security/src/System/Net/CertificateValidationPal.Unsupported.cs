// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable IDE0060

namespace System.Net
{
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext? securityContext,
            X509Chain chain,
            X509Certificate2? remoteCertificate,
            bool checkCertName,
            bool isServer,
            string? hostName) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext? securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain,
            X509ChainPolicy? chainPolicy) =>
            null;

        internal static bool IsLocalCertificateUsed(SafeFreeCredentials? credentialsHandle, SafeDeleteContext? securityContext) => false;

        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext) => Array.Empty<string>();

        static partial void CheckSupportsStore(StoreLocation storeLocation, ref bool hasSupport) => hasSupport = false;

        private static X509Store OpenStore(StoreLocation storeLocation) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
    }
}
