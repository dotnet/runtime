// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class OpenSslCertificateAssetDownloader
    {
        private static readonly Func<string, CancellationToken, byte[]?>? s_downloadBytes = CreateDownloadBytesFunc();

        internal static X509Certificate2? DownloadCertificate(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null || data.Length == 0)
            {
                return null;
            }

            try
            {
                X509Certificate2 certificate = new X509Certificate2(data);
                certificate.ThrowIfInvalid();
                return certificate;
            }
            catch (CryptographicException)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.InvalidDownloadedCertificate();
                }

                return null;
            }
        }

        internal static SafeX509CrlHandle? DownloadCrl(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null)
            {
                return null;
            }

            // DER-encoded CRL seems to be the most common off of some random spot-checking, so try DER first.
            SafeX509CrlHandle handle = Interop.Crypto.DecodeX509Crl(data, data.Length);

            if (!handle.IsInvalid)
            {
                return handle;
            }

            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                Interop.Crypto.BioWrite(bio, data, data.Length);

                handle = Interop.Crypto.PemReadBioX509Crl(bio);

                // DecodeX509Crl failed, so we need to clear its error.
                // If PemReadBioX509Crl failed, clear that too.
                Interop.Crypto.ErrClearError();

                if (!handle.IsInvalid)
                {
                    return handle;
                }
            }

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.InvalidDownloadedCrl();
            }

            return null;
        }

        internal static SafeOcspResponseHandle? DownloadOcspGet(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null)
            {
                return null;
            }

            // https://tools.ietf.org/html/rfc6960#appendix-A.2 says that the response is the DER-encoded
            // response, so no rebuffering to interpret PEM is required.
            SafeOcspResponseHandle resp = Interop.Crypto.DecodeOcspResponse(data);

            if (resp.IsInvalid)
            {
                // We're not going to report this error to a user, so clear it
                // (to avoid tainting future exceptions)
                Interop.Crypto.ErrClearError();

                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.InvalidDownloadedOcsp();
                }
            }

            return resp;
        }

        private static byte[]? DownloadAsset(string uri, TimeSpan downloadTimeout)
        {
            if (s_downloadBytes is null)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.HttpClientNotAvailable();
                }

                return null;
            }

            if (downloadTimeout <= TimeSpan.Zero)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.DownloadTimeExceeded();
                }

                return null;
            }

            long totalMillis = (long)downloadTimeout.TotalMilliseconds;

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.AssetDownloadStart(totalMillis, uri);
            }

            CancellationTokenSource? cts = totalMillis > int.MaxValue ? null : new CancellationTokenSource((int)totalMillis);
            byte[]? ret = null;

            try
            {
                ret = s_downloadBytes(uri, cts?.Token ?? default);
                return ret;
            }
            catch { }
            finally
            {
                cts?.Dispose();

                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.AssetDownloadStop(ret?.Length ?? 0);
                }
            }

            return null;
        }

        private static Func<string, CancellationToken, byte[]?>? CreateDownloadBytesFunc()
        {
            try
            {
                // Use reflection to access System.Net.Http:
                // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                // the latter can't in turn have an explicit dependency on the former.

                // Get the relevant types needed.
                Type? socketsHttpHandlerType = Type.GetType("System.Net.Http.SocketsHttpHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpMessageHandlerType = Type.GetType("System.Net.Http.HttpMessageHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpClientType = Type.GetType("System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpRequestMessageType = Type.GetType("System.Net.Http.HttpRequestMessage, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpResponseMessageType = Type.GetType("System.Net.Http.HttpResponseMessage, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpResponseHeadersType = Type.GetType("System.Net.Http.Headers.HttpResponseHeaders, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpContentType = Type.GetType("System.Net.Http.HttpContent, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (socketsHttpHandlerType == null || httpMessageHandlerType == null || httpClientType == null || httpRequestMessageType == null ||
                    httpResponseMessageType == null || httpResponseHeadersType == null || httpContentType == null)
                {
                    Debug.Fail("Unable to load required type.");
                    return null;
                }

                // Get the methods on those types.
                ConstructorInfo? socketsHttpHandlerCtor = socketsHttpHandlerType.GetConstructor(Type.EmptyTypes);
                PropertyInfo? pooledConnectionIdleTimeoutProp = socketsHttpHandlerType.GetProperty("PooledConnectionIdleTimeout");
                PropertyInfo? allowAutoRedirectProp = socketsHttpHandlerType.GetProperty("AllowAutoRedirect");
                ConstructorInfo? httpClientCtor = httpClientType.GetConstructor(new Type[] { httpMessageHandlerType });
                PropertyInfo? requestUriProp = httpRequestMessageType.GetProperty("RequestUri");
                ConstructorInfo? httpRequestMessageCtor = httpRequestMessageType.GetConstructor(Type.EmptyTypes);
                MethodInfo? sendMethod = httpClientType.GetMethod("Send", new Type[] { httpRequestMessageType, typeof(CancellationToken) });
                PropertyInfo? responseContentProp = httpResponseMessageType.GetProperty("Content");
                PropertyInfo? responseStatusCodeProp = httpResponseMessageType.GetProperty("StatusCode");
                PropertyInfo? responseHeadersProp = httpResponseMessageType.GetProperty("Headers");
                PropertyInfo? responseHeadersLocationProp = httpResponseHeadersType.GetProperty("Location");
                MethodInfo? readAsStreamMethod = httpContentType.GetMethod("ReadAsStream", Type.EmptyTypes);

                if (socketsHttpHandlerCtor == null || pooledConnectionIdleTimeoutProp == null || allowAutoRedirectProp == null || httpClientCtor == null ||
                    requestUriProp == null || httpRequestMessageCtor == null || sendMethod == null || responseContentProp == null || responseStatusCodeProp == null ||
                    responseHeadersProp == null || responseHeadersLocationProp == null || readAsStreamMethod == null)
                {
                    Debug.Fail("Unable to load required member.");
                    return null;
                }

                // Only keep idle connections around briefly, as a compromise between resource leakage and port exhaustion.
                const int PooledConnectionIdleTimeoutSeconds = 15;
                const int MaxRedirections = 10;

                // Equivalent of:
                // var socketsHttpHandler = new SocketsHttpHandler() {
                //     PooledConnectionIdleTimeout = TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds),
                //     AllowAutoRedirect = false
                // };
                // var httpClient = new HttpClient(socketsHttpHandler);
                // Note: using a ConstructorInfo instead of Activator.CreateInstance, so the ILLinker can see the usage through the lambda method.
                object? socketsHttpHandler = socketsHttpHandlerCtor.Invoke(null);
                pooledConnectionIdleTimeoutProp.SetValue(socketsHttpHandler, TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds));
                allowAutoRedirectProp.SetValue(socketsHttpHandler, false);
                object? httpClient = httpClientCtor.Invoke(new object?[] { socketsHttpHandler });

                return (string uriString, CancellationToken cancellationToken) =>
                {
                    Uri uri = new Uri(uriString);

                    if (!IsAllowedScheme(uri.Scheme))
                    {
                        return null;
                    }

                    // Equivalent of:
                    // HttpRequestMessage requestMessage = new HttpRequestMessage() { RequestUri = new Uri(uri) };
                    // HttpResponseMessage responseMessage = httpClient.Send(requestMessage, cancellationToken);
                    // Note: using a ConstructorInfo instead of Activator.CreateInstance, so the ILLinker can see the usage through the lambda method.
                    object requestMessage = httpRequestMessageCtor.Invoke(null);
                    requestUriProp.SetValue(requestMessage, uri);
                    object responseMessage = sendMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;

                    int redirections = 0;
                    Uri? redirectUri;
                    bool hasRedirect;
                    while (true)
                    {
                        int statusCode = (int)responseStatusCodeProp.GetValue(responseMessage)!;
                        object responseHeaders = responseHeadersProp.GetValue(responseMessage)!;
                        Uri? location = (Uri?)responseHeadersLocationProp.GetValue(responseHeaders);
                        redirectUri = GetUriForRedirect((Uri)requestUriProp.GetValue(requestMessage)!, statusCode, location, out hasRedirect);
                        if (redirectUri == null)
                        {
                            break;
                        }

                        ((IDisposable)responseMessage).Dispose();

                        redirections++;
                        if (redirections > MaxRedirections)
                        {
                            if (OpenSslX509ChainEventSource.Log.IsEnabled())
                            {
                                OpenSslX509ChainEventSource.Log.DownloadRedirectsExceeded();
                            }

                            return null;
                        }

                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.DownloadRedirected(redirectUri);
                        }

                        // Equivalent of:
                        // requestMessage = new HttpRequestMessage() { RequestUri = redirectUri };
                        // requestMessage.RequestUri = redirectUri;
                        // responseMessage = httpClient.Send(requestMessage, cancellationToken);
                        requestMessage = httpRequestMessageCtor.Invoke(null);
                        requestUriProp.SetValue(requestMessage, redirectUri);
                        responseMessage = sendMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                    }

                    if (hasRedirect && redirectUri == null)
                    {
                        return null;
                    }

                    // Equivalent of:
                    // using Stream responseStream = resp.Content.ReadAsStream();
                    object content = responseContentProp.GetValue(responseMessage)!;
                    using Stream responseStream = (Stream)readAsStreamMethod.Invoke(content, null)!;

                    var result = new MemoryStream();
                    responseStream.CopyTo(result);
                    ((IDisposable)responseMessage).Dispose();
                    return result.ToArray();
                };
            }
            catch
            {
                // We shouldn't have any exceptions, but if we do, ignore them all.
                return null;
            }
        }

        private static Uri? GetUriForRedirect(Uri requestUri, int statusCode, Uri? location, out bool hasRedirect)
        {
            if (!IsRedirectStatusCode(statusCode))
            {
                hasRedirect = false;
                return null;
            }

            hasRedirect = true;

            if (location == null)
            {
                return null;
            }

            // Ensure the redirect location is an absolute URI.
            if (!location.IsAbsoluteUri)
            {
                location = new Uri(requestUri, location);
            }

            // Per https://tools.ietf.org/html/rfc7231#section-7.1.2, a redirect location without a
            // fragment should inherit the fragment from the original URI.
            string requestFragment = requestUri.Fragment;
            if (!string.IsNullOrEmpty(requestFragment))
            {
                string redirectFragment = location.Fragment;
                if (string.IsNullOrEmpty(redirectFragment))
                {
                    location = new UriBuilder(location) { Fragment = requestFragment }.Uri;
                }
            }

            if (!IsAllowedScheme(location.Scheme))
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.DownloadRedirectNotFollowed(location);
                }

                return null;
            }

            return location;
        }

        private static bool IsRedirectStatusCode(int statusCode)
        {
            // MultipleChoices (300), Moved (301), Found (302), SeeOther (303), TemporaryRedirect (307), PermanentRedirect (308)
            return (statusCode >= 300 && statusCode <= 303) || statusCode == 307 || statusCode == 308;
        }

        private static bool IsAllowedScheme(string scheme)
        {
            return string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase);
        }
    }
}
