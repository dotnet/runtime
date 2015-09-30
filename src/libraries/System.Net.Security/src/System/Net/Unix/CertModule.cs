// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{   
    internal partial class CertModule : CertInterface
    {
        #region internal Methods
        internal override SslPolicyErrors VerifyCertificateProperties(
            X509Chain chain,
            X509Certificate2 certificate,
            bool checkCertName,
            bool isServer,
            string hostName)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;

            if (!chain.Build(certificate))
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            if (checkCertName)
            {
                if (string.IsNullOrEmpty(hostName))
                {
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                }
                else
                {
                    int hostnameMatch;

                    using (SafeX509Handle certHandle = Interop.libcrypto.X509_dup(certificate.Handle))
                    {
                        hostnameMatch = Interop.Crypto.CheckX509Hostname(certHandle, hostName, hostName.Length);
                    }

                    if (hostnameMatch != 1)
                    {
                        Debug.Assert(hostnameMatch == 0, "hostnameMatch should be (0,1) was " + hostnameMatch);
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                    }
                }
            }
            return sslPolicyErrors;
        }

        //
        // Extracts a remote certificate upon request.
        //
        internal override X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateStore)
        {
            remoteCertificateStore = null;
            bool gotReference = false;

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
                    remoteContext.DangerousAddRef(ref gotReference);
                    result = new X509Certificate2(remoteContext.DangerousGetHandle());
                }

                remoteCertificateStore = new X509Certificate2Collection();

                SafeSharedX509StackHandle chainStack =
                    Interop.OpenSsl.GetPeerCertificateChain(securityContext.DangerousGetHandle());

                GC.KeepAlive(securityContext);

                using (chainStack)
                {
                    if (!chainStack.IsInvalid)
                    {
                        int count = Interop.Crypto.GetX509StackFieldCount(chainStack);

                        for (int i = 0; i < count; i++)
                        {
                            IntPtr certPtr = Interop.Crypto.GetX509StackField(chainStack, i);

                            if (certPtr != IntPtr.Zero)
                            {
                                // X509Certificate2(IntPtr) calls X509_dup, so the reference is appropriately tracked.
                                X509Certificate2 chainCert = new X509Certificate2(certPtr);
                                remoteCertificateStore.Add(chainCert);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (gotReference)
                {
                    remoteContext.DangerousRelease();
                }

                if (remoteContext != null)
                {
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
            // TODO (Issue #3362) populate issuers
            string[] issuers = Array.Empty<string>();          

            return issuers;
        }

        internal override X509Store EnsureStoreOpened(bool isMachineStore)
        {
            // TODO (Issue #3362) do the implementation
            return null;
        }

        #endregion
     
    }

}
