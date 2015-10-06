// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal partial class CertModule : CertInterface
    {
        private static readonly object s_syncObject = new object();

        private static volatile X509Store s_myCertStoreEx;
        private static volatile X509Store s_myMachineCertStoreEx;

        #region internal Methods
        internal override SslPolicyErrors VerifyCertificateProperties(X509Chain chain, X509Certificate2 certificate, bool checkCertName, bool isServer, string hostName)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;

            if (!chain.Build(certificate)       // Build failed on handle or on policy.
                && chain.SafeHandle.DangerousGetHandle() == IntPtr.Zero)   // Build failed to generate a valid handle.
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            if (checkCertName)
            {
                unsafe
                {
                    uint status = 0;

                    var eppStruct = new Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA>(),
                        dwAuthType =
                            isServer
                                ? Interop.Crypt32.AuthType.AUTHTYPE_SERVER
                                : Interop.Crypt32.AuthType.AUTHTYPE_CLIENT,
                        fdwChecks = 0,
                        pwszServerName = null
                    };

                    var cppStruct = new Interop.Crypt32.CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_POLICY_PARA>(),
                        dwFlags = 0,
                        pvExtraPolicyPara = &eppStruct
                    };

                    fixed (char* namePtr = hostName)
                    {
                        eppStruct.pwszServerName = namePtr;
                        cppStruct.dwFlags |= (Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_ALL &
                                              ~Interop.Crypt32.CertChainPolicyIgnoreFlags
                                                  .CERT_CHAIN_POLICY_IGNORE_INVALID_NAME_FLAG);

                        SafeX509ChainHandle chainContext = chain.SafeHandle;

                        status = Verify(chainContext, ref cppStruct);

                        if (status == Interop.Crypt32.CertChainPolicyErrors.CERT_E_CN_NO_MATCH)
                        {
                            sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                        }
                    }
                }
            }

            X509ChainStatus[] chainStatusArray = chain.ChainStatus;
            if (chainStatusArray != null && chainStatusArray.Length != 0)
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return sslPolicyErrors;
        }

        //
        // Extracts a remote certificate upon request.
        //
        internal override X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateStore)
        {
            remoteCertificateStore = null;

            if (securityContext == null)
            {
                return null;
            }

            GlobalLog.Enter("SecureChannel#" + Logging.HashString(this) + "::RemoteCertificate{get;}");
            X509Certificate2 result = null;
            SafeFreeCertContext remoteContext = null;
            try
            {
                int errorCode = SSPIWrapper.QueryContextRemoteCertificate(GlobalSSPI.SSPISecureChannel, securityContext, out remoteContext);
                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    result = new X509Certificate2(remoteContext.DangerousGetHandle());
                }
            }
            finally
            {
                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    remoteCertificateStore = UnmanagedCertificateContext.GetRemoteCertificatesFromStoreContext(remoteContext);

                    remoteContext.Dispose();
                }
            }

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_remote_certificate, (result == null ? "null" : result.ToString(true))));
            }

            GlobalLog.Leave("SecureChannel#" + Logging.HashString(this) + "::RemoteCertificate{get;}", (result == null ? "null" : result.Subject));

            return result;
        }

        //
        // Used only by client SSL code, never returns null.
        //
        internal override string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            string[] issuers = Array.Empty<string>();

            object outObj;

            int errorCode = SSPIWrapper.QueryContextIssuerList(GlobalSSPI.SSPISecureChannel, securityContext, out outObj);

            GlobalLog.Assert(errorCode == 0, "QueryContextIssuerList returned errorCode:" + errorCode);

            Interop.Secur32.IssuerListInfoEx issuerList = (Interop.Secur32.IssuerListInfoEx)outObj;

            try
            {
                if (issuerList.cIssuers > 0)
                {
                    unsafe
                    {
                        uint count = issuerList.cIssuers;
                        issuers = new string[issuerList.cIssuers];
                        Interop.Secur32._CERT_CHAIN_ELEMENT* pIL = (Interop.Secur32._CERT_CHAIN_ELEMENT*)issuerList.aIssuers.DangerousGetHandle();
                        for (int i = 0; i < count; ++i)
                        {
                            Interop.Secur32._CERT_CHAIN_ELEMENT* pIL2 = pIL + i;
                            GlobalLog.Assert(pIL2->cbSize > 0, "SecureChannel::GetIssuers()", "Interop.Secur32._CERT_CHAIN_ELEMENT size is not positive: " + pIL2->cbSize.ToString());
                            if (pIL2->cbSize > 0)
                            {
                                uint size = pIL2->cbSize;
                                byte* ptr = (byte*)(pIL2->pCertContext);
                                byte[] x = new byte[size];
                                for (int j = 0; j < size; j++)
                                {
                                    x[j] = *(ptr + j);
                                }

                                X500DistinguishedName x500DistinguishedName = new X500DistinguishedName(x);
                                issuers[i] = x500DistinguishedName.Name;
                                GlobalLog.Print("SecureChannel#" + Logging.HashString(this) + "::GetIssuers() IssuerListEx[" + i + "]:" + issuers[i]);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (issuerList.aIssuers != null)
                {
                    issuerList.aIssuers.Dispose();
                }
            }

            return issuers;
        }

        //
        // Security: We temporarily reset thread token to open the cert store under process account.
        //
        internal override X509Store EnsureStoreOpened(bool isMachineStore)
        {
            X509Store store = isMachineStore ? s_myMachineCertStoreEx : s_myCertStoreEx;

            if (store == null)
            {
                lock (s_syncObject)
                {
                    store = isMachineStore ? s_myMachineCertStoreEx : s_myCertStoreEx;
                    if (store == null)
                    {
                        // NOTE: that if this call fails we won't keep track and the next time we enter we will try to open the store again.
                        StoreLocation storeLocation = isMachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
                        store = new X509Store(StoreName.My, storeLocation);
                        try
                        {
                            // For app-compat We want to ensure the store is opened under the **process** account.
                            try
                            {
                                WindowsIdentity.RunImpersonated(SafeAccessTokenHandle.InvalidHandle, () =>
                                {
                                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                                    GlobalLog.Print("SecureChannel::EnsureStoreOpened() storeLocation:" + storeLocation + " returned store:" + store.GetHashCode().ToString("x"));
                                });
                            }
                            catch
                            {
                                throw;
                            }

                            if (isMachineStore)
                            {
                                s_myMachineCertStoreEx = store;
                            }
                            else
                            {
                                s_myCertStoreEx = store;
                            }

                            return store;
                        }
                        catch (Exception exception)
                        {
                            if (exception is CryptographicException || exception is SecurityException)
                            {
                                GlobalLog.Assert("SecureChannel::EnsureStoreOpened()", "Failed to open cert store, location:" + storeLocation + " exception:" + exception);
                                return null;
                            }

                            if (Logging.On)
                            {
                                Logging.PrintError(Logging.Web, SR.Format(SR.net_log_open_store_failed, storeLocation, exception));
                            }

                            throw;
                        }
                    }
                }
            }
            return store;
        }
        #endregion

        #region Private Methods

        private uint Verify(SafeX509ChainHandle chainContext, ref Interop.Crypt32.CERT_CHAIN_POLICY_PARA cpp)
        {
            GlobalLog.Enter("SecureChannel::VerifyChainPolicy", "chainContext=" + chainContext + ", options=" + String.Format("0x{0:x}", cpp.dwFlags));
            var status = new Interop.Crypt32.CERT_CHAIN_POLICY_STATUS();
            status.cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_POLICY_STATUS>();

            bool errorCode = Interop.Crypt32.CertVerifyCertificateChainPolicy((IntPtr)Interop.Crypt32.CertChainPolicy.CERT_CHAIN_POLICY_SSL, chainContext, ref cpp, ref status);

            GlobalLog.Print("SecureChannel::VerifyChainPolicy() CertVerifyCertificateChainPolicy returned: " + errorCode);
#if TRACE_VERBOSE
            GlobalLog.Print("SecureChannel::VerifyChainPolicy() error code: " + status.dwError + String.Format(" [0x{0:x8}", status.dwError) + " " + Interop.MapSecurityStatus(status.dwError) + "]");
#endif
            GlobalLog.Leave("SecureChannel::VerifyChainPolicy", status.dwError.ToString());
            return status.dwError;
        }

        #endregion
    }
}
