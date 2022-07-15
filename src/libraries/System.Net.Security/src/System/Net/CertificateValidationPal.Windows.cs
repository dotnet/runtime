// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace System.Net
{
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext? securityContext,
            X509Chain chain,
            X509Certificate2 remoteCertificate,
            bool checkCertName,
            bool isServer,
            string? hostName)
        {
            return CertificateValidation.BuildChainAndVerifyProperties(chain, remoteCertificate, checkCertName, isServer, hostName);
        }

        //
        // Extracts a remote certificate upon request.
        //

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext? securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain,
            X509ChainPolicy? chainPolicy)
        {
            if (securityContext == null)
            {
                return null;
            }

            X509Certificate2? result = null;
            SafeFreeCertContext? remoteContext = null;
            try
            {
                // SECPKG_ATTR_REMOTE_CERT_CONTEXT will not succeed before TLS handshake completes. Inside the handshake,
                // we need to use (more expensive) SECPKG_ATTR_REMOTE_CERT_CHAIN. That one may be unsupported on older
                // versions of windows. In that case, we have no option than to return null.
                //
                // We can use retrieveCollection to distinguish between in-handshake and after-handshake calls, because
                // the collection is retrieved for cert validation purposes after the handshake completes.
                if (retrieveChainCertificates) // handshake completed
                {
                    SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CONTEXT(GlobalSSPI.SSPISecureChannel, securityContext, out remoteContext);
                }
                else // in handshake
                {
                    SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CHAIN(GlobalSSPI.SSPISecureChannel, securityContext, out remoteContext);
                }

                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    result = new X509Certificate2(remoteContext.DangerousGetHandle());
                }
            }
            finally
            {
                if (remoteContext != null)
                {
                    if (!remoteContext.IsInvalid)
                    {
                        if (retrieveChainCertificates)
                        {
                            chain ??= new X509Chain();
                            if (chainPolicy != null)
                            {
                                chain.ChainPolicy = chainPolicy;
                            }

                            UnmanagedCertificateContext.GetRemoteCertificatesFromStoreContext(remoteContext, chain.ChainPolicy.ExtraStore);
                        }
                    }

                    remoteContext.Dispose();
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.RemoteCertificate(result);
            return result;
        }

        //
        // Used only by client SSL code, never returns null.
        //
        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            Interop.SspiCli.SecPkgContext_IssuerListInfoEx issuerList = default;
            bool success = SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_ISSUER_LIST_EX(GlobalSSPI.SSPISecureChannel, securityContext, ref issuerList, out SafeHandle? sspiHandle);

            string[] issuers = Array.Empty<string>();
            try
            {
                if (success && issuerList.cIssuers > 0)
                {
                    unsafe
                    {
                        issuers = new string[issuerList.cIssuers];
                        var elements = new Span<Interop.SspiCli.CERT_CHAIN_ELEMENT>((void*)sspiHandle!.DangerousGetHandle(), issuers.Length);
                        for (int i = 0; i < elements.Length; ++i)
                        {
                            Debug.Assert(elements[i].cbSize > 0, $"Interop.SspiCli._CERT_CHAIN_ELEMENT size is not positive: {elements[i].cbSize}");
                            if (elements[i].cbSize > 0)
                            {
                                byte[] x = new Span<byte>((byte*)elements[i].pCertContext, checked((int)elements[i].cbSize)).ToArray();
                                var x500DistinguishedName = new X500DistinguishedName(x);
                                issuers[i] = x500DistinguishedName.Name;
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(securityContext, $"IssuerListEx[{issuers[i]}]");
                            }
                        }
                    }
                }
            }
            finally
            {
                sspiHandle?.Dispose();
            }

            return issuers;
        }

        //
        // Security: We temporarily reset thread token to open the cert store under process account.
        //
        internal static X509Store OpenStore(StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);

            // For app-compat We want to ensure the store is opened under the **process** account.
            try
            {
                using SafeAccessTokenHandle invalidHandle = SafeAccessTokenHandle.InvalidHandle;
                WindowsIdentity.RunImpersonated(invalidHandle, () =>
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                });
            }
            catch
            {
                throw;
            }

            return store;
        }
    }
}
