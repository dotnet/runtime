// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        internal sealed class ClientCertificateProvider : IDisposable
        {
            internal readonly GCHandle _gcHandle;
            internal readonly Interop.Ssl.ClientCertCallback _callback;
            private readonly X509Certificate2Collection _clientCertificates;

            internal ClientCertificateProvider(X509Certificate2Collection clientCertificates)
            {
                _gcHandle = GCHandle.Alloc(this);
                _callback = TlsClientCertCallback;
                _clientCertificates = clientCertificates;
            }

            private int TlsClientCertCallback(IntPtr ssl, out IntPtr certHandle, out IntPtr privateKeyHandle)
            {
                EventSourceTrace("SSL: {0}", ssl);
                const int CertificateSet = 1, NoCertificateSet = 0, SuspendHandshake = -1;

                certHandle = IntPtr.Zero;
                privateKeyHandle = IntPtr.Zero;

                if (ssl == IntPtr.Zero)
                {
                    Debug.Fail("Expected valid SSL pointer");
                    EventSourceTrace("Invalid SSL pointer in callback");
                    return NoCertificateSet;
                }

                SafeSslHandle sslHandle = null;
                X509Chain chain = null;
                X509Certificate2 certificate = null;
                try
                {
                    sslHandle = new SafeSslHandle(ssl, ownsHandle: false);

                    ISet<string> issuerNames = GetRequestCertificateAuthorities(sslHandle);

                    if (_clientCertificates != null) // manual mode
                    {
                        // If there's one certificate, just use it. Otherwise, try to find the best one.
                        int certCount = _clientCertificates.Count;
                        if (certCount == 1)
                        {
                            EventSourceTrace("Single certificate.  Building chain.");
                            certificate = _clientCertificates[0];
                            chain = TLSCertificateExtensions.BuildNewChain(certificate, includeClientApplicationPolicy: false);
                        }
                        else
                        {
                            EventSourceTrace("Finding the best of {0} certificates", certCount);
                            if (!_clientCertificates.TryFindClientCertificate(issuerNames, out certificate, out chain))
                            {
                                EventSourceTrace("No certificate set.");
                                return NoCertificateSet;
                            }
                        }
                        EventSourceTrace("Chain built.");
                    }
                    else if (!GetAutomaticClientCertificate(issuerNames, out certificate, out chain)) // automatic mode
                    {
                        EventSourceTrace("No automatic certificate or chain.");
                        return NoCertificateSet;
                    }

                    SafeEvpPKeyHandle privateKeySafeHandle = null;
                    Interop.Crypto.CheckValidOpenSslHandle(certificate.Handle);
                    using (RSAOpenSsl rsa = certificate.GetRSAPrivateKey() as RSAOpenSsl)
                    {
                        if (rsa != null)
                        {
                            privateKeySafeHandle = rsa.DuplicateKeyHandle();
                            EventSourceTrace("RSA key");
                        }
                        else
                        {
                            using (ECDsaOpenSsl ecdsa = certificate.GetECDsaPrivateKey() as ECDsaOpenSsl)
                            {
                                if (ecdsa != null)
                                {
                                    privateKeySafeHandle = ecdsa.DuplicateKeyHandle();
                                    EventSourceTrace("ECDsa key");
                                }
                            }
                        }
                    }

                    if (privateKeySafeHandle == null || privateKeySafeHandle.IsInvalid)
                    {
                        EventSourceTrace("Invalid private key");
                        return NoCertificateSet;
                    }

                    SafeX509Handle certSafeHandle = Interop.Crypto.X509UpRef(certificate.Handle);
                    Interop.Crypto.CheckValidOpenSslHandle(certSafeHandle);
                    if (chain != null)
                    {
                        if (!Interop.Ssl.AddExtraChainCertificates(sslHandle, chain))
                        {
                            EventSourceTrace("Failed to add extra chain certificate");
                            return SuspendHandshake;
                        }
                    }

                    certHandle = certSafeHandle.DangerousGetHandle();
                    privateKeyHandle = privateKeySafeHandle.DangerousGetHandle();
                    EventSourceTrace("Client certificate set: {0}", certificate);

                    // Ownership has been transferred to OpenSSL; do not free these handles
                    certSafeHandle.SetHandleAsInvalid();
                    privateKeySafeHandle.SetHandleAsInvalid();

                    return CertificateSet;
                }
                finally
                {
                    if (_clientCertificates == null) certificate?.Dispose(); // only dispose cert if it's automatic / newly created
                    chain?.Dispose();
                    sslHandle?.Dispose();
                }
            }

            public void Dispose()
            {
                _gcHandle.Free();
            }

            private static ISet<string> GetRequestCertificateAuthorities(SafeSslHandle sslHandle)
            {
                using (SafeSharedX509NameStackHandle names = Interop.Ssl.SslGetClientCAList(sslHandle))
                {
                    if (names.IsInvalid)
                    {
                        return new HashSet<string>();
                    }

                    int nameCount = Interop.Crypto.GetX509NameStackFieldCount(names);
                    var clientAuthorityNames = new HashSet<string>(nameCount);
                    for (int i = 0; i < nameCount; i++)
                    {
                        using (SafeSharedX509NameHandle nameHandle = Interop.Crypto.GetX509NameStackField(names, i))
                        {
                            X500DistinguishedName dn = Interop.Crypto.LoadX500Name(nameHandle);
                            clientAuthorityNames.Add(dn.Name);
                        }
                    }
                    return clientAuthorityNames;
                }
            }

            private static bool GetAutomaticClientCertificate(ISet<string> allowedIssuers, out X509Certificate2 certificate, out X509Chain chain)
            {
                using (X509Store myStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    // Get the certs from the store.
                    myStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certs = myStore.Certificates;

                    // Find a matching one.
                    bool gotCert = certs.TryFindClientCertificate(allowedIssuers, out certificate, out chain);

                    // Dispose all but the matching cert.
                    for (int i = 0; i < certs.Count; i++)
                    {
                        X509Certificate2 cert = certs[i];
                        if (cert != certificate)
                        {
                            cert.Dispose();
                        }
                    }

                    // Return whether we got one.
                    return gotCert;
                }
            }
        }
    }
}
