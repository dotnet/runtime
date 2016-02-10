// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

using CURLcode = Interop.Http.CURLcode;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        internal sealed class ClientCertificateProvider : IDisposable
        {
            internal readonly GCHandle _gcHandle;
            internal readonly Interop.Ssl.ClientCertCallback _callback;
            private SafeEvpPKeyHandle _privateKeyHandle = null;
            private SafeX509Handle _certHandle = null;

            internal ClientCertificateProvider ()
            {
                _gcHandle = GCHandle.Alloc(this);
                _callback = TlsClientCertCallback;
            }

            private int TlsClientCertCallback(IntPtr ssl, out IntPtr certHandle, out IntPtr privateKeyHandle)
            {
                Interop.Crypto.CheckValidOpenSslHandle(ssl);
                using (SafeSslHandle sslHandle = new SafeSslHandle(ssl, false))
                {
                    certHandle = IntPtr.Zero;
                    privateKeyHandle = IntPtr.Zero;

                    ISet<string> issuerNames = GetRequestCertificateAuthorities(sslHandle);
                    X509Certificate2 certificate;
                    X509Chain chain;
                    if (!GetClientCertificate(issuerNames, out certificate, out chain))
                    {
                        EventSourceTrace("No certificate or chain");
                        return 0;
                    }

                    Interop.Crypto.CheckValidOpenSslHandle(certificate.Handle);
                    using (RSAOpenSsl rsa = certificate.GetRSAPrivateKey() as RSAOpenSsl)
                    {
                        if (rsa != null)
                        {
                            _privateKeyHandle = rsa.DuplicateKeyHandle();
                            EventSourceTrace("RSA key");
                        }
                        else
                        {
                            using (ECDsaOpenSsl ecdsa = certificate.GetECDsaPrivateKey() as ECDsaOpenSsl)
                            {
                                if (ecdsa != null)
                                {
                                    _privateKeyHandle = ecdsa.DuplicateKeyHandle();
                                    EventSourceTrace("ECDsa key");
                                }
                            }
                        }
                    }

                    if (_privateKeyHandle == null || _privateKeyHandle.IsInvalid)
                    {
                        EventSourceTrace("Invalid private key");
                        return 0;
                    }

                    _certHandle = Interop.Crypto.X509Duplicate(certificate.Handle);
                    Interop.Crypto.CheckValidOpenSslHandle(_certHandle);
                    if (chain != null)
                    {
                        for (int i = chain.ChainElements.Count - 2; i > 0; i--)
                        {
                            SafeX509Handle dupCertHandle = Interop.Crypto.X509Duplicate(chain.ChainElements[i].Certificate.Handle);
                            Interop.Crypto.CheckValidOpenSslHandle(dupCertHandle);
                            if (!Interop.Ssl.SslAddExtraChainCert(sslHandle, dupCertHandle))
                            {
                                EventSourceTrace("Failed to add extra chain certificate");
                                return -1;
                            }
                        }
                    }

                    certHandle = _certHandle.DangerousGetHandle();
                    privateKeyHandle = _privateKeyHandle.DangerousGetHandle();
                    return 1;
                }
            }

            public void Dispose()
            {
                _gcHandle.Free();
                if (_privateKeyHandle != null)
                {
                    _privateKeyHandle.Dispose();
                }

                if (_certHandle != null)
                {
                    _certHandle.Dispose();
                }
            }

            private static ISet<string> GetRequestCertificateAuthorities(SafeSslHandle sslHandle)
            {
                HashSet<string> clientAuthorityNames = new HashSet<string>();
                using (SafeSharedX509NameStackHandle names = Interop.Ssl.SslGetClientCAList(sslHandle))
                {
                    if (names.IsInvalid)
                    {
                        return clientAuthorityNames;
                    }

                    int nameCount = Interop.Crypto.GetX509NameStackFieldCount(names);

                    if (nameCount == 0)
                    {
                        return clientAuthorityNames;
                    }

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

            private static bool GetClientCertificate(ISet<string> allowedIssuers, out X509Certificate2 certificate, out X509Chain chain)
            {
                using (X509Store myStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    myStore.Open(OpenFlags.ReadOnly);
                    return myStore.Certificates.TryFindClientCertificate(allowedIssuers, out certificate, out chain);
                }
            }
        }
    }
}
