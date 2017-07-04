// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using CURLcode = Interop.Http.CURLcode;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private static class SslProvider
        {
            internal static void SetSslOptions(EasyRequest easy, ClientCertificateOption clientCertOption)
            {
                Debug.Assert(
                    clientCertOption == ClientCertificateOption.Automatic ||
                    clientCertOption == ClientCertificateOption.Manual);

                // Create a client certificate provider if client certs may be used.
                X509Certificate2Collection clientCertificates = easy._handler._clientCertificates;

                if (clientCertOption != ClientCertificateOption.Manual || clientCertificates?.Count > 0)
                {
                    // libcurl does not have an option of accepting a SecIdentityRef via an input option,
                    // only via writing it to a file and letting it load the PFX.
                    // This would require that a) we write said file, and b) that it contaminate the default
                    // keychain (because their PFX loader loads to the default keychain).
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.net_http_libcurl_clientcerts_notsupported,
                            CurlVersionDescription,
                            CurlSslVersionDescription));
                }

                // Revocation checking is always on for darwinssl (SecureTransport).
                // If any other backend is used and revocation is requested, we can't guarantee
                // that assertion.
                if (easy._handler.CheckCertificateRevocationList &&
                    !CurlSslVersionDescription.Equals(Interop.Http.SecureTransportDescription))
                {
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.net_http_libcurl_revocation_notsupported,
                            CurlVersionDescription,
                            CurlSslVersionDescription));
                }

                if (easy._handler.ServerCertificateCustomValidationCallback != null)
                {
                    // libcurl (as of 7.49.1) does not have any callback which can be registered which fires
                    // between the time that a TLS/SSL handshake has offered up the server certificate and the
                    // time that the HTTP request headers are written.  Were there any callback, the option
                    // CURLINFO_TLS_SSL_PTR could be queried (and the backend identifier validated to be
                    // CURLSSLBACKEND_DARWINSSL). Then the SecTrustRef could be extracted to build the chain,
                    // a la SslStream.
                    //
                    // Without the callback the matrix looks like:
                    // * If default-trusted and callback-would-trust: No difference (except side effects, like logging).
                    // * If default-trusted and callback-would-block: Data would have been sent in violation of user trust.
                    // * If not-default-trusted and callback-would-not-trust: No difference (except side effects).
                    // * If not-default-trusted and callback-would-trust: No data sent, which doesn't match user desires.
                    //
                    // Of the two "different" cases, sending when we shouldn't is worse, so that's the direction we
                    // have to cater to. So we'll use default trust, and throw on any custom callback.
                    //
                    // The situation where system trust fails can be remedied by including the certificate into the
                    // user's keychain and setting the SSL policy trust for it to "Always Trust".
                    // Similarly, the "block this" could be attained by setting the SSL policy for a cert in the
                    // keychain to "Never Trust".
                    //
                    // However, one case we can support is when we know all certificates will pass validation.
                    // We can detect a key case of that: whether DangerousAcceptAnyServerCertificateValidator was used.
                    if (easy.ServerCertificateValidationCallbackAcceptsAll)
                    {
                        EventSourceTrace("Warning: Disabling peer verification per {0}", nameof(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator), easy: easy);
                        easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYPEER, 0); // don't verify the peer

                        // Don't set CURLOPT_SSL_VERIFHOST to 0; doing so disables SNI with SecureTransport backend.
                        if (!CurlSslVersionDescription.Equals(Interop.Http.SecureTransportDescription))
                        {
                            easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_SSL_VERIFYHOST, 0); // don't verify the hostname
                        }
                    }
                    else
                    {
                        throw new PlatformNotSupportedException(
                            SR.Format(
                                SR.net_http_libcurl_callback_notsupported,
                                CurlVersionDescription,
                                CurlSslVersionDescription));
                    }
                }

                SetSslVersion(easy);
            }

            private static void SetSslVersion(EasyRequest easy)
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
        }
    }
}
