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

            if (checkCertName)
            {
                System.Diagnostics.Debug.Assert(hostName != null);
                SafeDeleteSslContext sslContext = (SafeDeleteSslContext)securityContext;
                if (!Interop.AndroidCrypto.SSLStreamVerifyHostname(sslContext.SslContext, hostName!))
                {
                    errors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                }
            }

            return errors;
        }

        //
        // Extracts a remote certificate upon request.
        //

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain)
        {
            SafeSslHandle sslContext = ((SafeDeleteSslContext)securityContext).SslContext;
            if (sslContext == null)
                return null;

            X509Certificate2? cert = null;
            if (!retrieveChainCertificates)
            {
                // Constructing a new X509Certificate2 adds a global reference to the pointer, so we dispose this handle
                using (SafeX509Handle handle = Interop.AndroidCrypto.SSLStreamGetPeerCertificate(sslContext))
                {
                    if (!handle.IsInvalid)
                    {
                        cert = new X509Certificate2(handle.DangerousGetHandle());
                    }
                }
            }
            else
            {
                chain ??= new X509Chain();
                IntPtr[]? ptrs = Interop.AndroidCrypto.SSLStreamGetPeerCertificates(sslContext);
                if (ptrs != null && ptrs.Length > 0)
                {
                    // This is intentionally a different object from the cert added to the remote certificate store
                    // to match the behaviour on other platforms.
                    cert = new X509Certificate2(ptrs[0]);
                    foreach (IntPtr ptr in ptrs)
                    {
                        // Constructing a new X509Certificate2 adds a global reference to the pointer, so we dispose this handle
                        using (var handle = new SafeX509Handle(ptr))
                        {
                            chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(handle.DangerousGetHandle()));
                        }
                    }

                }
            }

            return cert;
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
