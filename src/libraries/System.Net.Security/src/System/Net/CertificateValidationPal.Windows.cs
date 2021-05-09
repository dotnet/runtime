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
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;

            bool chainBuildResult = chain.Build(remoteCertificate);
            if (!chainBuildResult       // Build failed on handle or on policy.
                && chain.SafeHandle!.DangerousGetHandle() == IntPtr.Zero)   // Build failed to generate a valid handle.
            {
                throw new CryptographicException(Marshal.GetLastPInvokeError());
            }

            if (checkCertName)
            {
                unsafe
                {
                    uint status = 0;

                    var eppStruct = new Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)sizeof(Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA),
                        // Authenticate the remote party: (e.g. when operating in server mode, authenticate the client).
                        dwAuthType = isServer ? Interop.Crypt32.AuthType.AUTHTYPE_CLIENT : Interop.Crypt32.AuthType.AUTHTYPE_SERVER,
                        fdwChecks = 0,
                        pwszServerName = null
                    };

                    var cppStruct = new Interop.Crypt32.CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_PARA),
                        dwFlags = 0,
                        pvExtraPolicyPara = &eppStruct
                    };

                    fixed (char* namePtr = hostName)
                    {
                        eppStruct.pwszServerName = namePtr;
                        cppStruct.dwFlags |=
                            (Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_ALL &
                             ~Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_INVALID_NAME_FLAG);

                        SafeX509ChainHandle chainContext = chain.SafeHandle!;
                        status = Verify(chainContext, ref cppStruct);
                        if (status == Interop.Crypt32.CertChainPolicyErrors.CERT_E_CN_NO_MATCH)
                        {
                            sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                        }
                    }
                }
            }

            if (!chainBuildResult)
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return sslPolicyErrors;
        }

        //
        // Extracts a remote certificate upon request.
        //

        internal static X509Certificate2? GetRemoteCertificate(SafeDeleteContext? securityContext) =>
            GetRemoteCertificate(securityContext, retrieveCollection: false, out _);

        internal static X509Certificate2? GetRemoteCertificate(SafeDeleteContext? securityContext, out X509Certificate2Collection? remoteCertificateCollection) =>
            GetRemoteCertificate(securityContext, retrieveCollection: true, out remoteCertificateCollection);

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext? securityContext, bool retrieveCollection, out X509Certificate2Collection? remoteCertificateCollection)
        {
            remoteCertificateCollection = null;

            if (securityContext == null)
            {
                return null;
            }

            X509Certificate2? result = null;
            SafeFreeCertContext? remoteContext = null;
            try
            {
                remoteContext = SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CONTEXT(GlobalSSPI.SSPISecureChannel, securityContext);
                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    result = new X509Certificate2(remoteContext.DangerousGetHandle());
                }
            }
            finally
            {
                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    if (retrieveCollection)
                    {
                        remoteCertificateCollection = UnmanagedCertificateContext.GetRemoteCertificatesFromStoreContext(remoteContext);
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
                WindowsIdentity.RunImpersonated(SafeAccessTokenHandle.InvalidHandle, () =>
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

        private static unsafe uint Verify(SafeX509ChainHandle chainContext, ref Interop.Crypt32.CERT_CHAIN_POLICY_PARA cpp)
        {
            Interop.Crypt32.CERT_CHAIN_POLICY_STATUS status = default;
            status.cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);

            bool errorCode =
                Interop.Crypt32.CertVerifyCertificateChainPolicy(
                    (IntPtr)Interop.Crypt32.CertChainPolicy.CERT_CHAIN_POLICY_SSL,
                    chainContext,
                    ref cpp,
                    ref status);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(chainContext, $"CertVerifyCertificateChainPolicy returned: {errorCode}. Status: {status.dwError}");
            return status.dwError;
        }
    }
}
