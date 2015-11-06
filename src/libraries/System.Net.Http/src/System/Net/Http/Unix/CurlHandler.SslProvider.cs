// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

using CURLcode = Interop.Http.CURLcode;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private static class SslProvider
        {
            private static readonly Interop.Http.SslCtxCallback s_sslCtxCallback = SetSslCtxVerifyCallback;
            private static readonly Interop.Ssl.AppVerifyCallback s_sslVerifyCallback = VerifyCertChain;

            internal static void SetSslOptions(EasyRequest easy)
            {
                CURLcode answer = easy.SetSslCtxCallback(s_sslCtxCallback);

                switch (answer)
                {
                    case CURLcode.CURLE_OK:
                        break;
                    // Curl 7.38 and prior
                    case CURLcode.CURLE_UNKNOWN_OPTION:
                    // Curl 7.39 and later
                    case CURLcode.CURLE_NOT_BUILT_IN:
                        VerboseTrace("CURLOPT_SSL_CTX_FUNCTION is not supported, platform default https chain building in use");
                        break;
                    default:
                        ThrowIfCURLEError(answer);
                        break;
                }
            }

            private static CURLcode SetSslCtxVerifyCallback(
                IntPtr curl,
                IntPtr sslCtx)
            {
                using (SafeSslContextHandle ctx = new SafeSslContextHandle(sslCtx, ownsHandle: false))
                {
                    Interop.Ssl.SslCtxSetCertVerifyCallback(ctx, s_sslVerifyCallback, IntPtr.Zero);
                }

                return CURLcode.CURLE_OK;
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
