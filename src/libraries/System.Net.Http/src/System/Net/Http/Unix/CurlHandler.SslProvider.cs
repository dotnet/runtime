// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

using CURLcode = Interop.Http.CURLcode;
using CURLINFO = Interop.Http.CURLINFO;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private static class SslProvider
        {
            private static readonly Interop.Http.SslCtxCallback s_sslCtxCallback = SetSslCtxVerifyCallback;
            private static readonly Interop.Ssl.AppVerifyCallback s_sslVerifyCallback = VerifyCertChain;

            internal static void SetSslOptions(EasyRequest easy, ClientCertificateOption clientCertOption)
            {
                // Disable SSLv2/SSLv3, allow TLSv1.*
                easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSLVERSION, (long)Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1);

                IntPtr userPointer = IntPtr.Zero;
                if (clientCertOption == ClientCertificateOption.Automatic)
                {
                    ClientCertificateProvider certProvider = new ClientCertificateProvider();
                    userPointer = GCHandle.ToIntPtr(certProvider._gcHandle);
                    easy.Task.ContinueWith((_, state) => ((IDisposable)state).Dispose(),
                                           certProvider,
                                           CancellationToken.None,
                                           TaskContinuationOptions.ExecuteSynchronously,
                                           TaskScheduler.Default);
                }
                else
                {
                    Debug.Assert(clientCertOption == ClientCertificateOption.Manual, "ClientCertificateOption is manual or automatic");
                }

                CURLcode answer = easy.SetSslCtxCallback(s_sslCtxCallback, userPointer);

                switch (answer)
                {
                    case CURLcode.CURLE_OK:
                        break;
                    // Curl 7.38 and prior
                    case CURLcode.CURLE_UNKNOWN_OPTION:
                    // Curl 7.39 and later
                    case CURLcode.CURLE_NOT_BUILT_IN:
                        EventSourceTrace("CURLOPT_SSL_CTX_FUNCTION not supported. Platform default HTTPS chain building in use");
                        if (clientCertOption == ClientCertificateOption.Automatic)
                        {
                            throw new PlatformNotSupportedException(SR.net_http_unix_invalid_client_cert_option);
                        }

                        break;
                    default:
                        ThrowIfCURLEError(answer);
                        break;
                }
            }

            private static CURLcode SetSslCtxVerifyCallback(
                IntPtr curl,
                IntPtr sslCtx,
                IntPtr userPointer)
            {
                using (SafeSslContextHandle ctx = new SafeSslContextHandle(sslCtx, ownsHandle: false))
                {
                    Interop.Ssl.SslCtxSetCertVerifyCallback(ctx, s_sslVerifyCallback, curl);

                    if (userPointer == IntPtr.Zero)
                    {
                        EventSourceTrace("Not using client certificate callback");
                    }
                    else
                    {
                        ClientCertificateProvider provider = null;
                        try
                        {
                            GCHandle handle = GCHandle.FromIntPtr(userPointer);
                            provider = (ClientCertificateProvider)handle.Target;
                        }
                        catch (InvalidCastException)
                        {
                            Debug.Fail("ClientCertificateProvider wasn't the GCHandle's Target");
                            return CURLcode.CURLE_ABORTED_BY_CALLBACK;
                        }
                        catch (InvalidOperationException)
                        {
                            Debug.Fail("Invalid GCHandle in CurlSslCallback");
                            return CURLcode.CURLE_ABORTED_BY_CALLBACK;
                        }

                        Debug.Assert(provider != null, "Expected non-null sslCallback in curlCallBack");
                        Interop.Ssl.SslCtxSetClientCertCallback(ctx, provider._callback);
                    }
                }
                return CURLcode.CURLE_OK;
            }

            private static void AddChannelBindingToken(X509Certificate2 certificate, IntPtr curlPtr)
            {
                Debug.Assert(curlPtr != IntPtr.Zero, "curlPtr is not null");
                IntPtr gcHandlePtr;
                CURLcode getInfoResult = Interop.Http.EasyGetInfoPointer(curlPtr, CURLINFO.CURLINFO_PRIVATE, out gcHandlePtr);
                Debug.Assert(getInfoResult == CURLcode.CURLE_OK, "Failed to get info on a completing easy handle");
                if (getInfoResult == CURLcode.CURLE_OK)
                {
                    GCHandle handle = GCHandle.FromIntPtr(gcHandlePtr);
                    EasyRequest easy = handle.Target as EasyRequest;
                    Debug.Assert(easy != null, "Expected non-null EasyRequest in GCHandle");
                    if (easy._requestContentStream != null)
                    {
                        easy._requestContentStream.SetChannelBindingToken(certificate);
                    }
                }
            }

            private static int VerifyCertChain(IntPtr storeCtxPtr, IntPtr arg)
            {
                using (SafeX509StoreCtxHandle storeCtx = new SafeX509StoreCtxHandle(storeCtxPtr, ownsHandle: false))
                {
                    // First use the default verification provided directly by OpenSSL.
                    // If it succeeds in verifying the cert chain, we're done.
                    // (Employing this instead of our custom implementation will need to be
                    // revisited if we ever decide to a) introduce a "disallowed" store
                    // that enables users to "untrust" certs the system trusts, or b) decide
                    // CRL checking is required, neither of which is done by OpenSSL).
                    int sslResult = Interop.Crypto.X509VerifyCert(storeCtx);
                    if (sslResult == 1)
                    {
                        return 1;
                    }

                    // X509_verify_cert can return < 0 in the case of programmer error
                    Debug.Assert(sslResult == 0, "Unexpected error from X509_verify_cert: " + sslResult);

                    // Only if the fast default verification fails do we then fall back to our more 
                    // manual and more expensive verification that includes checking the user's 
                    // certs and not just the system store ones.
                    List<X509Certificate2> otherCerts;
                    bool success;
                    using (X509Chain chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                        IntPtr leafCertPtr = Interop.Crypto.X509StoreCtxGetTargetCert(storeCtx);

                        if (IntPtr.Zero == leafCertPtr)
                        {
                            Debug.Fail("Invalid target certificate");
                            return -1;
                        }

                        using (SafeSharedX509StackHandle extraStack = Interop.Crypto.X509StoreCtxGetSharedUntrusted(storeCtx))
                        {
                            int extraSize = extraStack.IsInvalid ? 0 : Interop.Crypto.GetX509StackFieldCount(extraStack);
                            otherCerts = new List<X509Certificate2>(extraSize);

                            for (int i = 0; i < extraSize; i++)
                            {
                                IntPtr certPtr = Interop.Crypto.GetX509StackField(extraStack, i);

                                if (certPtr != IntPtr.Zero)
                                {
                                    X509Certificate2 cert = new X509Certificate2(certPtr);
                                    otherCerts.Add(cert);
                                    chain.ChainPolicy.ExtraStore.Add(cert);
                                }
                            }
                        }

                        using (X509Certificate2 leafCert = new X509Certificate2(leafCertPtr))
                        {
                            success = chain.Build(leafCert);
                            AddChannelBindingToken(leafCert, arg);
                        }
                    }

                    foreach (X509Certificate2 otherCert in otherCerts)
                    {
                        otherCert.Dispose();
                    }

                    return success ? 1 : 0;
                }
            }
        }
    }
}
