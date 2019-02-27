// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
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
            private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");
            private static string _sslCaPath;
            private static string _sslCaInfo;

            internal static void SetSslOptions(EasyRequest easy, ClientCertificateOption clientCertOption)
            {
                EventSourceTrace("ClientCertificateOption: {0}", clientCertOption, easy:easy);
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
                    EventSourceTrace("Created certificate provider", easy:easy);

                    // The client cert provider needs to be passed through to the callback, and thus
                    // we create a GCHandle to keep it rooted.  This handle needs to be cleaned up
                    // when the request has completed, and a simple and pay-for-play way to do that
                    // is by cleaning it up in a continuation off of the request.
                    userPointer = GCHandle.ToIntPtr(certProvider._gcHandle);
                    easy.Task.ContinueWith((_, state) => ((IDisposable)state).Dispose(), certProvider,
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }

                // Configure the options.  Our best support is when targeting OpenSSL/1.0.  For other backends,
                // we fall back to a minimal amount of support, and may throw a PNSE based on the options requested.
                if (Interop.Http.HasMatchingOpenSslVersion)
                {
                    // Register the callback with libcurl.  We need to register even if there's no user-provided
                    // server callback and even if there are no client certificates, because we support verifying
                    // server certificates against more than those known to OpenSSL.
                    SetSslOptionsForSupportedBackend(easy, certProvider, userPointer);
                }
                else
                {
                    // Newer versions of OpenSSL, and other non-OpenSSL backends, do not currently support callbacks.
                    // That means we'll throw a PNSE if a callback is required.
                    SetSslOptionsForUnsupportedBackend(easy, certProvider);
                }
            }

            private static void SetSslOptionsForSupportedBackend(EasyRequest easy, ClientCertificateProvider certProvider, IntPtr userPointer)
            {
                CURLcode answer = easy.SetSslCtxCallback(s_sslCtxCallback, userPointer);
                EventSourceTrace("Callback registration result: {0}", answer, easy: easy);
                switch (answer)
                {
                    case CURLcode.CURLE_OK:
                        // We successfully registered.  If we'll be invoking a user-provided callback to verify the server
                        // certificate as part of that, disable libcurl's verification of the host name; we need to get
                        // the callback from libcurl even if the host name doesn't match, so we take on the responsibility
                        // of doing the host name match in the callback prior to invoking the user's delegate.
                        if (easy._handler.ServerCertificateCustomValidationCallback != null)
                        {
                            easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYHOST, 0);
                            // But don't change the CURLOPT_SSL_VERIFYPEER setting, as setting it to 0 will
                            // cause SSL and libcurl to ignore the result of the server callback.
                        }

                        SetSslOptionsForCertificateStore(easy);

                        // The allowed SSL protocols will be set in the configuration callback.
                        break;

                    case CURLcode.CURLE_UNKNOWN_OPTION: // Curl 7.38 and prior
                    case CURLcode.CURLE_NOT_BUILT_IN:   // Curl 7.39 and later
                        SetSslOptionsForUnsupportedBackend(easy, certProvider);
                        break;

                    default:
                        ThrowIfCURLEError(answer);
                        break;
                }
            }

            private static void GetSslCaLocations(out string path, out string info)
            {
                // We only provide curl option values when SSL_CERT_FILE or SSL_CERT_DIR is set.
                // When that is the case, we set the options so curl ends up using the same certificates as the
                // X509 machine store.
                path = _sslCaPath;
                info = _sslCaInfo;

                if (path == null || info == null)
                {
                    bool hasEnvironmentVariables = Environment.GetEnvironmentVariable("SSL_CERT_FILE") != null ||
                                                   Environment.GetEnvironmentVariable("SSL_CERT_DIR") != null;

                    if (hasEnvironmentVariables)
                    {
                        path = Interop.Crypto.GetX509RootStorePath();
                        if (!Directory.Exists(path))
                        {
                            // X509 store ignores non-existing.
                            path = string.Empty;
                        }

                        info = Interop.Crypto.GetX509RootStoreFile();
                        if (!File.Exists(info))
                        {
                            // X509 store ignores non-existing.
                            info = string.Empty;
                        }
                    }
                    else
                    {
                        path = string.Empty;
                        info = string.Empty;
                    }
                    _sslCaPath = path;
                    _sslCaInfo = info;
                }
            }

            private static void SetSslOptionsForCertificateStore(EasyRequest easy)
            {
                // Support specifying certificate directory/bundle via environment variables: SSL_CERT_DIR, SSL_CERT_FILE.
                GetSslCaLocations(out string sslCaPath, out string sslCaInfo);

                if (sslCaPath != string.Empty)
                {
                    easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_CAPATH, sslCaPath);

                    // https proxy support requires libcurl 7.52.0+
                    easy.TrySetCurlOption(Interop.Http.CURLoption.CURLOPT_PROXY_CAPATH, sslCaPath);
                }

                if (sslCaInfo != string.Empty)
                {
                    easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_CAINFO, sslCaInfo);

                    // https proxy support requires libcurl 7.52.0+
                    easy.TrySetCurlOption(Interop.Http.CURLoption.CURLOPT_PROXY_CAINFO, sslCaInfo);
                }
            }

            private static void SetSslOptionsForUnsupportedBackend(EasyRequest easy, ClientCertificateProvider certProvider)
            {
                if (certProvider != null)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_http_libcurl_clientcerts_notsupported_sslbackend, CurlVersionDescription, CurlSslVersionDescription, Interop.Http.RequiredOpenSslDescription));
                }

                if (easy._handler.CheckCertificateRevocationList)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_http_libcurl_revocation_notsupported_sslbackend, CurlVersionDescription, CurlSslVersionDescription, Interop.Http.RequiredOpenSslDescription));
                }

                if (easy._handler.ServerCertificateCustomValidationCallback != null)
                {
                    if (easy.ServerCertificateValidationCallbackAcceptsAll)
                    {
                        EventSourceTrace("Warning: Disabling peer and host verification per {0}", nameof(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator), easy: easy);
                        easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYPEER, 0);
                        easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYHOST, 0);
                    }
                    else
                    {
                        throw new PlatformNotSupportedException(SR.Format(SR.net_http_libcurl_callback_notsupported_sslbackend, CurlVersionDescription, CurlSslVersionDescription, Interop.Http.RequiredOpenSslDescription));
                    }
                }
                else
                {
                    SetSslOptionsForCertificateStore(easy);
                }

                // In case of defaults configure the allowed SSL protocols.
                SetSslVersion(easy);
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

                // libcurl supports options for either enabling all of the TLS1.* protocols or enabling 
                // just one protocol; it doesn't currently support enabling two of the three, e.g. you can't 
                // pick TLS1.1 and TLS1.2 but not TLS1.0, but you can select just TLS1.2.
                Interop.Http.CurlSslVersion curlSslVersion;
                switch (protocols)
                {
#pragma warning disable 0618 // SSL2/3 are deprecated
                    case SslProtocols.Ssl2:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_SSLv2;
                        break;
                    case SslProtocols.Ssl3:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_SSLv3;
                        break;
#pragma warning restore 0618

                    case SslProtocols.Tls:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_0;
                        break;
                    case SslProtocols.Tls11:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_1;
                        break;
                    case SslProtocols.Tls12:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_2;
                        break;
                    case SslProtocols.Tls13:
                        curlSslVersion = Interop.Http.CurlSslVersion.CURL_SSLVERSION_TLSv1_3;
                        break;

                    case SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12:
                    case SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13:
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
                EventSourceTrace(null, easy: easy);

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
                const int SuccessResult = 1, FailureResult = 0;

                EasyRequest easy;
                if (!TryGetEasyRequest(curlPtr, out easy))
                {
                    EventSourceTrace("Could not find associated easy request: {0}", curlPtr);
                    return FailureResult;
                }

                var storeCtx = new SafeX509StoreCtxHandle(storeCtxPtr, ownsHandle: false);
                try
                {
                    return VerifyCertChain(storeCtx, easy) ? SuccessResult : FailureResult;
                }
                catch (Exception exc)
                {
                    EventSourceTrace("Unexpected exception: {0}", exc, easy: easy);
                    easy.FailRequest(CreateHttpRequestException(new CurlException((int)CURLcode.CURLE_ABORTED_BY_CALLBACK, exc)));
                    return FailureResult;
                }
                finally
                {
                    storeCtx.Dispose();
                }
            }

            private static bool VerifyCertChain(SafeX509StoreCtxHandle storeCtx, EasyRequest easy)
            {
                IntPtr leafCertPtr = Interop.Crypto.X509StoreCtxGetTargetCert(storeCtx);
                if (leafCertPtr == IntPtr.Zero)
                {
                    EventSourceTrace("Invalid certificate pointer", easy: easy);
                    return false;
                }

                X509Certificate2[] otherCerts = null;
                int otherCertsCount = 0;
                var leafCert = new X509Certificate2(leafCertPtr);
                try
                {
                    // We need to respect the user's server validation callback if there is one.  If there isn't one,
                    // we can start by first trying to use OpenSSL's verification, though only if CRL checking is disabled,
                    // as OpenSSL doesn't do that.
                    if (easy._handler.ServerCertificateCustomValidationCallback == null &&
                        !easy._handler.CheckCertificateRevocationList)
                    {
                        // Start by using the default verification provided directly by OpenSSL.
                        // If it succeeds in verifying the cert chain, we're done. Employing this instead of 
                        // our custom implementation will need to be revisited if we ever decide to introduce a 
                        // "disallowed" store that enables users to "untrust" certs the system trusts.
                        if (Interop.Crypto.X509VerifyCert(storeCtx))
                        {
                            return true;
                        }
                    }

                    // Either OpenSSL verification failed, or there was a server validation callback
                    // or certificate revocation checking was enabled. Either way, fall back to manual
                    // and more expensive verification that includes checking the user's certs (not
                    // just the system store ones as OpenSSL does).
                    using (var chain = new X509Chain())
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
                            return errors == SslPolicyErrors.None;
                        }
                        else
                        {
                            // Authenticate the remote party: (e.g. when operating in client mode, authenticate the server).
                            chain.ChainPolicy.ApplicationPolicy.Add(s_serverAuthOid);

                            SslPolicyErrors errors = CertificateValidation.BuildChainAndVerifyProperties(chain, leafCert,
                                checkCertName: true, hostName: easy._requestMessage.RequestUri.Host); // we disabled automatic host verification, so we do it here
                            return serverCallback(easy._requestMessage, leafCert, chain, errors);
                        }
                    }
                }
                finally
                {
                    for (int i = 0; i < otherCertsCount; i++)
                    {
                        otherCerts[i].Dispose();
                    }
                    leafCert.Dispose();
                }
            }
        }
    }
}
