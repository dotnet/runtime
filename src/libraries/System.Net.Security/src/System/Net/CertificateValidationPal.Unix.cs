// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext? _ /*securityContext*/,
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
            IntPtr remoteCertificate = Interop.OpenSsl.GetPeerCertificate((SafeSslHandle)securityContext);
            try
            {
                if (remoteCertificate == IntPtr.Zero)
                {
                    return null;
                }

                result = new X509Certificate2(remoteCertificate);

                if (retrieveChainCertificates)
                {
                    chain ??= new X509Chain();
                    if (chainPolicy != null)
                    {
                        chain.ChainPolicy = chainPolicy;
                    }

                    using (SafeSharedX509StackHandle chainStack =
                        Interop.OpenSsl.GetPeerCertificateChain((SafeSslHandle)securityContext))
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
                                    chain.ChainPolicy.ExtraStore.Add(chainCert);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                result?.Dispose();
                throw;
            }
            finally
            {
                if (remoteCertificate != IntPtr.Zero)
                {
                    // Creating X509Certificate will increase the reference count
                    // so we need to release it explicitly on either success or failure.
                    Interop.Crypto.X509Destroy(remoteCertificate);
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.RemoteCertificate(result);
            return result;
        }

        internal static bool IsLocalCertificateUsed(SafeFreeCredentials? _1, SafeDeleteContext? ctx)
        {
            if (ctx is not SafeSslHandle ssl)
            {
                return false;
            }

            if (!Interop.Ssl.SslSessionReused(ssl))
            {
                // Fresh session, we set the certificate on the SSL object only
                // if the peer explicitly requested it.
                return Interop.Ssl.SslGetCertificate(ssl) != IntPtr.Zero;
            }

            // resumed session, we keep the information about cert being used in the SSL_SESSION
            // object's ex_data
            bool addref = false;
            try
            {
                // make sure the ssl is not freed while we accessing its SSL_SESSION
                // this makes sure the `session` pointer is valid during this call
                // despite not being a SafeHandle.
                ssl.DangerousAddRef(ref addref);

                // the information about certificate usage is stored in the session ex data
                IntPtr session = Interop.Ssl.SslGetSession(ssl);
                Debug.Assert(session != IntPtr.Zero);
                return Interop.Ssl.SslSessionGetData(session) != IntPtr.Zero;
            }
            finally
            {
                if (addref)
                {
                    ssl.DangerousRelease();
                }
            }
        }

        //
        // Used only by client SSL code, never returns null.
        //
        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            using (SafeSharedX509NameStackHandle names = Interop.Ssl.SslGetClientCAList((SafeSslHandle)securityContext))
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
            // https://github.com/dotnet/runtime/issues/15377 tracks the lack of this store.
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
    }
}
