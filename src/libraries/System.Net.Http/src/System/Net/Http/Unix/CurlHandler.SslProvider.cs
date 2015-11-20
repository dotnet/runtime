// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
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
                        VerboseTrace("CURLOPT_SSL_CTX_FUNCTION is not supported, platform default https chain building in use");
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
                        VerboseTrace("Not using client Certificate callback ");
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
                List<X509Certificate2> otherCerts;
                bool success;

                using (SafeX509StoreCtxHandle storeCtx = new SafeX509StoreCtxHandle(storeCtxPtr, ownsHandle: false))
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
