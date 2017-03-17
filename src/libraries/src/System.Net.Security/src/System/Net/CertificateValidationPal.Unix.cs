// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{   
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext securityContext,
            X509Chain chain,
            X509Certificate2 remoteCertificate,
            bool checkCertName,
            bool isServer,
            string hostName)
        {
            return CertificateValidation.BuildChainAndVerifyProperties(chain, remoteCertificate, checkCertName, hostName);
        }

        //
        // Extracts a remote certificate upon request.
        //
        internal static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext)
        {
            return GetRemoteCertificate(securityContext, null);
        }

        internal static X509Certificate2 GetRemoteCertificate(
            SafeDeleteContext securityContext,
            out X509Certificate2Collection remoteCertificateStore)
        {
            if (securityContext == null)
            {
                remoteCertificateStore = null;
                return null;
            }

            remoteCertificateStore = new X509Certificate2Collection();
            return GetRemoteCertificate(securityContext, remoteCertificateStore);
        }

        private static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, X509Certificate2Collection remoteCertificateStore)
        {
            bool gotReference = false;

            if (securityContext == null)
            {
                return null;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(securityContext);

            X509Certificate2 result = null;
            SafeFreeCertContext remoteContext = null;
            try
            {
                int errorCode = QueryContextRemoteCertificate(securityContext, out remoteContext);

                if (remoteContext != null && !remoteContext.IsInvalid)
                {
                    remoteContext.DangerousAddRef(ref gotReference);
                    result = new X509Certificate2(remoteContext.DangerousGetHandle());
                }

                if (remoteCertificateStore != null)
                {
                    using (SafeSharedX509StackHandle chainStack =
                        Interop.OpenSsl.GetPeerCertificateChain(((SafeDeleteSslContext)securityContext).SslContext))
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

            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Log.RemoteCertificate(result);
                NetEventSource.Exit(securityContext, result);
            }
            return result;
        }      

        //
        // Used only by client SSL code, never returns null.
        //
        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            using (SafeSharedX509NameStackHandle names = Interop.Ssl.SslGetClientCAList(((SafeDeleteSslContext)securityContext).SslContext))
            {
                if (names.IsInvalid)
                {
                    return Array.Empty<string>();
                }

                int nameCount = Interop.Crypto.GetX509NameStackFieldCount(names);

                if (nameCount == 0)
                {
                    return Array.Empty<string>();
                }

                string[] clientAuthorityNames = new string[nameCount];

                for (int i = 0; i < nameCount; i++)
                {
                    using (SafeSharedX509NameHandle nameHandle = Interop.Crypto.GetX509NameStackField(names, i))
                    {
                        X500DistinguishedName dn = Interop.Crypto.LoadX500Name(nameHandle);
                        clientAuthorityNames[i] = dn.Name;
                    }
                }

                return clientAuthorityNames;
            }
        }

        static partial void CheckSupportsStore(StoreLocation storeLocation, ref bool hasSupport)
        {
            // There's not currently a LocalMachine\My store on Unix, so don't bother trying
            // and having to deal with the exception.
            //
            // https://github.com/dotnet/corefx/issues/3690 tracks the lack of this store.
            if (storeLocation == StoreLocation.LocalMachine)
                hasSupport = false;
        }

        private static X509Store OpenStore(StoreLocation storeLocation)
        {
            Debug.Assert(storeLocation == StoreLocation.CurrentUser);

            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            return store;
        }

        private static int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCertContext)
        {
            remoteCertContext = null;
            try
            {
                SafeX509Handle remoteCertificate = Interop.OpenSsl.GetPeerCertificate(((SafeDeleteSslContext)securityContext).SslContext);
                // Note that cert ownership is transferred to SafeFreeCertContext
                remoteCertContext = new SafeFreeCertContext(remoteCertificate);
                return 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}
