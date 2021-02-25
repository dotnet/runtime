// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext securityContext,
            X509Chain chain,
            X509Certificate2? remoteCertificate,
            bool checkCertName,
            bool isServer,
            string? hostName)
        {
            if (remoteCertificate == null)
                return  SslPolicyErrors.RemoteCertificateNotAvailable;

            SslPolicyErrors errors = chain.Build(remoteCertificate)
                ? SslPolicyErrors.None
                : SslPolicyErrors.RemoteCertificateChainErrors;

            if (!checkCertName)
                return errors;

            throw new NotImplementedException(nameof(VerifyCertificateProperties));
        }

        //
        // Extracts a remote certificate upon request.
        //
        internal static X509Certificate2? GetRemoteCertificate(SafeDeleteContext securityContext)
        {
            return GetRemoteCertificate(securityContext, null);
        }

        internal static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext? securityContext,
            out X509Certificate2Collection? remoteCertificateStore)
        {
            if (securityContext == null)
            {
                remoteCertificateStore = null;
                return null;
            }

            remoteCertificateStore = new X509Certificate2Collection();
            return GetRemoteCertificate(securityContext, remoteCertificateStore);
        }

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext securityContext,
            X509Certificate2Collection? remoteCertificateStore)
        {
            if (securityContext == null)
                return null;

            SafeSslHandle sslContext = ((SafeDeleteSslContext)securityContext).SslContext;
            if (sslContext == null)
                return null;

            throw new NotImplementedException(nameof(GetRemoteCertificate));
        }

        //
        // Used only by client SSL code, never returns null.
        //
        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            SafeSslHandle sslContext = ((SafeDeleteSslContext)securityContext).SslContext;
            if (sslContext == null)
                return Array.Empty<string>();

            throw new NotImplementedException(nameof(GetRequestCertificateAuthorities));
        }

        private static X509Store OpenStore(StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            return store;
        }
    }
}
