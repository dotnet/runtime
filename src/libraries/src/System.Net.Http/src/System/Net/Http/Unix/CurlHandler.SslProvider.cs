// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
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
            private static readonly Interop.Http.SslCtxCallback s_sslCtxCallback = SslCtxCallback;
            private static readonly Interop.Ssl.AppVerifyCallback s_sslVerifyCallback = VerifyCertChain;

            internal static void SetSslOptions(EasyRequest easy, ClientCertificateOption clientCertOption)
            {
                Debug.Assert(clientCertOption == ClientCertificateOption.Automatic || clientCertOption == ClientCertificateOption.Manual);

                // Create a client certificate provider if client certs may be used.
                X509Certificate2Collection clientCertificates = easy._handler._clientCertificates;
                ClientCertificateProvider certProvider =
                    clientCertOption == ClientCertificateOption.Automatic ? new ClientCertificateProvider(null) : // automatic
                    clientCertificates?.Count > 0 ? new ClientCertificateProvider(clientCertificates) : // manual with certs
                    null; // manual without certs
                IntPtr userPointer = IntPtr.Zero;
                if (certProvider != null)
                {
                    // The client cert provider needs to be passed through to the callback, and thus
                    // we create a GCHandle to keep it rooted.  This handle needs to be cleaned up
                    // when the request has completed, and a simple and pay-for-play way to do that
                    // is by cleaning it up in a continuation off of the request.
                    userPointer = GCHandle.ToIntPtr(certProvider._gcHandle);
                    easy.Task.ContinueWith((_, state) => ((IDisposable)state).Dispose(), certProvider,
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }

                // Register the callback with libcurl.  We need to register even if there's no user-provided
                // server callback and even if there are no client certificates, because we support verifying
                // server certificates against more than those known to OpenSSL.
                CURLcode answer = easy.SetSslCtxCallback(s_sslCtxCallback, userPointer);
                switch (answer)
                {
                    case CURLcode.CURLE_OK:
                        // We successfully registered.  If we'll be invoking a user-provided callback to verify the server
                        // certificate as part of that, disable libcurl's verification of the host name.  The user's callback
                        // needs to be given the opportunity to examine the cert, and our logic will determine whether
                        // the host name matches and will inform the callback of that.
                        if (easy._handler.ServerCertificateValidationCallback != null)
                        {
                            easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYHOST, 0); // don't verify the peer cert's hostname
                            // We don't change the SSL_VERIFYPEER setting, as setting it to 0 will cause
                            // SSL and libcurl to ignore the result of the server callback.
                        }

                        // The allowed SSL protocols will be set in the configuration callback.
                        break;

                    case CURLcode.CURLE_UNKNOWN_OPTION: // Curl 7.38 and prior
                    case CURLcode.CURLE_NOT_BUILT_IN:   // Curl 7.39 and later
                        // It's ok if we failed to register the callback if all of the defaults are in play
                        // with relation to handling of certificates.  But if that's not the case, failing to 
                        // register the callback will result in those options not being factored in, which is
                        // a significant enough error that we need to fail.
                        EventSourceTrace("CURLOPT_SSL_CTX_FUNCTION not supported: {0}", answer, easy: easy);
                        if (certProvider != null ||
                            easy._handler.ServerCertificateValidationCallback != null ||
                            easy._handler.CheckCertificateRevocationList)
                        {
                            throw new PlatformNotSupportedException(
                                SR.Format(SR.net_http_unix_invalid_certcallback_option, CurlVersionDescription, CurlSslVersionDescription));
                        }

                        // Since there won't be a callback to configure the allowed SSL protocols, configure them here.
                        SetSslVersion(easy);

                        break;

                    default:
                        ThrowIfCURLEError(answer);
                        break;
                }
            }

            private static void SetSslVersion(EasyRequest easy, IntPtr sslCtx = default(IntPtr))
            {
                // Get the requested protocols.
                SslProtocols protocols = easy._handler.SslProtocols;
                if (protocols == SslProtocols.None)
                {
                    // Let libcurl use its defaults if None is set.
                    return;
                }

                // We explicitly disallow choosing SSL2/3. Make sure they were filtered out.
                Debug.Assert((protocols & ~SecurityProtocol.AllowedSecurityProtocols) == 0, 
                    "Disallowed protocols should have been filtered out.");

                // libcurl supports options for either enabling all of the TLS1.* protocols or enabling 
                // just one of them; it doesn't currently support enabling two of the three, e.g. you can't 
                // pick TLS1.1 and TLS1.2 but not TLS1.0, but you can select just TLS1.2.
                Interop.Http.CurlSslVersion curlSslVersion;
                switch (protocols)
                {
                    case SslProtocols.Tls:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_0;
                        break;
                    case SslProtocols.Tls11:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_1;
                        break;
                    case SslProtocols.Tls12:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_2;
                        break;

                    case SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1;
                        break;

                    default:
                        throw new NotSupportedException(SR.net_securityprotocolnotsupported);
                }

                try
                {
                    easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSLVERSION, (long)curlSslVersion);
                }
                catch (CurlException e) when (e.HResult == (int)CURLcode.CURLE_UNKNOWN_OPTION)
                {
                    throw new NotSupportedException(SR.net_securityprotocolnotsupported, e);
                }
            }

            private static CURLcode SslCtxCallback(IntPtr curl, IntPtr sslCtx, IntPtr userPointer)
            {
                EasyRequest easy;
                if (!TryGetEasyRequest(curl, out easy))
                {
                    return CURLcode.CURLE_ABORTED_BY_CALLBACK;
                }

                // Configure the SSL protocols allowed.
                SslProtocols protocols = easy._handler.SslProtocols;
                if (protocols == SslProtocols.None)
                {
                    // If None is selected, let OpenSSL use its defaults, but with SSL2/3 explicitly disabled.
                    // Since the shim/OpenSSL work on a disabling system, where any protocols for which bits aren't
                    // set are disabled, we set all of the bits other than those we want disabled.
#pragma warning disable 0618 // the enum values are obsolete
                    protocols = ~(SslProtocols.Ssl2 | SslProtocols.Ssl3);
#pragma warning restore 0618
                }
                Interop.Ssl.SetProtocolOptions(sslCtx, protocols);

                // Configure the SSL server certificate verification callback.
                Interop.Ssl.SslCtxSetCertVerifyCallback(sslCtx, s_sslVerifyCallback, curl);

                // If a client certificate provider was provided, also configure the client certificate callback.
                if (userPointer != IntPtr.Zero)
                {
                    try
                    {
                        // Provider is passed in via a GCHandle.  Get the provider, which contains
                        // the client certificate callback delegate.
                        GCHandle handle = GCHandle.FromIntPtr(userPointer);
                        ClientCertificateProvider provider = (ClientCertificateProvider)handle.Target;
                        if (provider == null)
                        {
                            Debug.Fail($"Expected non-null provider in {nameof(SslCtxCallback)}");
                            return CURLcode.CURLE_ABORTED_BY_CALLBACK;
                        }

                        // Register the callback.
                        Interop.Ssl.SslCtxSetClientCertCallback(sslCtx, provider._callback);
                        EventSourceTrace("Registered client certificate callback.", easy: easy);
                    }
                    catch (Exception e)
                    {
                        Debug.Fail($"Exception in {nameof(SslCtxCallback)}", e.ToString());
                        return CURLcode.CURLE_ABORTED_BY_CALLBACK;
                    }
                }

                return CURLcode.CURLE_OK;
            }

            private static bool TryGetEasyRequest(IntPtr curlPtr, out EasyRequest easy)
            {
                Debug.Assert(curlPtr != IntPtr.Zero, "curlPtr is not null");

                IntPtr gcHandlePtr;
                CURLcode getInfoResult = Interop.Http.EasyGetInfoPointer(curlPtr, CURLINFO.CURLINFO_PRIVATE, out gcHandlePtr);
                if (getInfoResult == CURLcode.CURLE_OK)
                {
                    return MultiAgent.TryGetEasyRequestFromGCHandle(gcHandlePtr, out easy);
                }

                Debug.Fail($"Failed to get info on a completing easy handle: {getInfoResult}");
                easy = null;
                return false;
            }

            private static int VerifyCertChain(IntPtr storeCtxPtr, IntPtr curlPtr)
            {
                EasyRequest easy;
                if (!TryGetEasyRequest(curlPtr, out easy))
                {
                    EventSourceTrace("Could not find associated easy request: {0}", curlPtr);
                    return 0;
                }

                using (var storeCtx = new SafeX509StoreCtxHandle(storeCtxPtr, ownsHandle: false))
                {
                    IntPtr leafCertPtr = Interop.Crypto.X509StoreCtxGetTargetCert(storeCtx);
                    if (IntPtr.Zero == leafCertPtr)
                    {
                        EventSourceTrace("Invalid certificate pointer", easy: easy);
                        return 0;
                    }

                    using (X509Certificate2 leafCert = new X509Certificate2(leafCertPtr))
                    {
                        // We need to respect the user's server validation callback if there is one.  If there isn't one,
                        // we can start by first trying to use OpenSSL's verification, though only if CRL checking is disabled,
                        // as OpenSSL doesn't do that.
                        if (easy._handler.ServerCertificateValidationCallback == null &&
                            !easy._handler.CheckCertificateRevocationList)
                        {
                            // Start by using the default verification provided directly by OpenSSL.
                            // If it succeeds in verifying the cert chain, we're done. Employing this instead of 
                            // our custom implementation will need to be revisited if we ever decide to introduce a 
                            // "disallowed" store that enables users to "untrust" certs the system trusts.
                            int sslResult = Interop.Crypto.X509VerifyCert(storeCtx);
                            if (sslResult == 1)
                            {
                                return 1;
                            }

                            // X509_verify_cert can return < 0 in the case of programmer error
                            Debug.Assert(sslResult == 0, "Unexpected error from X509_verify_cert: " + sslResult);
                        }

                        // Either OpenSSL verification failed, or there was a server validation callback.
                        // Either way, fall back to manual and more expensive verification that includes 
                        // checking the user's certs (not just the system store ones as OpenSSL does).
                        X509Certificate2[] otherCerts;
                        int otherCertsCount = 0;
                        bool success;
                        using (X509Chain chain = new X509Chain())
                        {
                            chain.ChainPolicy.RevocationMode = easy._handler.CheckCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                            using (SafeSharedX509StackHandle extraStack = Interop.Crypto.X509StoreCtxGetSharedUntrusted(storeCtx))
                            {
                                if (extraStack.IsInvalid)
                                {
                                    otherCerts = Array.Empty<X509Certificate2>();
                                }
                                else
                                {
                                    int extraSize = Interop.Crypto.GetX509StackFieldCount(extraStack);
                                    otherCerts = new X509Certificate2[extraSize];

                                    for (int i = 0; i < extraSize; i++)
                                    {
                                        IntPtr certPtr = Interop.Crypto.GetX509StackField(extraStack, i);
                                        if (certPtr != IntPtr.Zero)
                                        {
                                            X509Certificate2 cert = new X509Certificate2(certPtr);
                                            otherCerts[otherCertsCount++] = cert;
                                            chain.ChainPolicy.ExtraStore.Add(cert);
                                        }
                                    }
                                }
                            }

                            var serverCallback = easy._handler._serverCertificateValidationCallback;
                            if (serverCallback == null)
                            {
                                SslPolicyErrors errors = CertificateValidation.BuildChainAndVerifyProperties(chain, leafCert,
                                    checkCertName: false, hostName: null); // libcurl already verifies the host name
                                success = errors == SslPolicyErrors.None;
                            }
                            else
                            {
                                SslPolicyErrors errors = CertificateValidation.BuildChainAndVerifyProperties(chain, leafCert,
                                    checkCertName: true, hostName: easy._requestMessage.RequestUri.Host); // we disabled automatic host verification, so we do it here
                                try
                                {
                                    success = serverCallback(easy._requestMessage, leafCert, chain, errors);
                                }
                                catch (Exception exc)
                                {
                                    EventSourceTrace("Server validation callback threw exception: {0}", exc, easy: easy);
                                    easy.FailRequest(exc);
                                    success = false;
                                }
                            }
                        }

                        for (int i = 0; i < otherCertsCount; i++)
                        {
                            otherCerts[i].Dispose();
                        }

                        return success ? 1 : 0;
                    }
                }
            }
        }
    }
}
