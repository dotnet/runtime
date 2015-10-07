// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{   
    internal partial class CertModule : CertInterface
    {
        private static readonly object s_lockObject = new object();
        private static X509Store s_userCertStore;

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

                using (SafeSharedX509StackHandle chainStack =
                    Interop.OpenSsl.GetPeerCertificateChain(securityContext.SslContext))
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
            if (isMachineStore)
            {
                // There's not currently a LocalMachine\My store on Unix, so don't bother trying
                // and having to deal with the exception.
                //
                // https://github.com/dotnet/corefx/issues/3690 tracks the lack of this store.
                return null;
            }

            return EnsureStoreOpened(ref s_userCertStore, StoreLocation.CurrentUser);
        }

        private static X509Store EnsureStoreOpened(ref X509Store storeField, StoreLocation storeLocation)
        {
            X509Store store = Volatile.Read(ref storeField);

            if (store == null)
            {
                lock (s_lockObject)
                {
                    store = Volatile.Read(ref storeField);

                    if (store == null)
                    {
                        try
                        {
                            store = new X509Store(StoreName.My, storeLocation);
                            store.Open(OpenFlags.ReadOnly);

                            Volatile.Write(ref storeField, store);

                            GlobalLog.Print(
                                "CertModule::EnsureStoreOpened() storeLocation:" + storeLocation +
                                    " returned store:" + store.GetHashCode().ToString("x"));
                        }
                        catch (CryptographicException e)
                        {
                            GlobalLog.Assert(
                                "CertModule::EnsureStoreOpened()",
                                "Failed to open cert store, location:" + storeLocation + " exception:" + e);

                            throw;
                        }
                    }
                }
            }

            return store;
        }

        #endregion
     
    }

}
