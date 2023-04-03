// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class X509ResourceClient
    {
        private static readonly Func<string, CancellationToken, bool, ValueTask<byte[]?>>? s_downloadBytes = CreateDownloadBytesFunc();

        static partial void ReportNoClient();
        static partial void ReportNegativeTimeout();
        static partial void ReportDownloadStart(long totalMillis, string uri);
        static partial void ReportDownloadStop(int bytesDownloaded);
        static partial void ReportRedirectsExceeded();
        static partial void ReportRedirected(Uri newUri);
        static partial void ReportRedirectNotFollowed(Uri redirectUri);

        internal static byte[]? DownloadAsset(string uri, TimeSpan downloadTimeout)
        {
            ValueTask<byte[]?> task = DownloadAssetCore(uri, downloadTimeout, async: false);
            Debug.Assert(task.IsCompletedSuccessfully);
            return task.Result;
        }

        internal static Task<byte[]?> DownloadAssetAsync(string uri, TimeSpan downloadTimeout)
        {
            ValueTask<byte[]?> task = DownloadAssetCore(uri, downloadTimeout, async: true);
            return task.AsTask();
        }

        private static async ValueTask<byte[]?> DownloadAssetCore(string uri, TimeSpan downloadTimeout, bool async)
        {
            if (s_downloadBytes is null)
            {
                ReportNoClient();

                return null;
            }

            if (downloadTimeout <= TimeSpan.Zero)
            {
                ReportNegativeTimeout();

                return null;
            }

            long totalMillis = (long)downloadTimeout.TotalMilliseconds;

            ReportDownloadStart(totalMillis, uri);

            CancellationTokenSource? cts = totalMillis > int.MaxValue ? null : new CancellationTokenSource((int)totalMillis);
            byte[]? ret = null;

            try
            {
                ret = await s_downloadBytes(uri, cts?.Token ?? default, async).ConfigureAwait(false);
                return ret;
            }
            catch { }
            finally
            {
                cts?.Dispose();

                ReportDownloadStop(ret?.Length ?? 0);
            }

            return null;
        }

        private static Func<string, CancellationToken, bool, ValueTask<byte[]?>>? CreateDownloadBytesFunc()
        {
            try
            {
                // Use reflection to access System.Net.Http:
                // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                // the latter can't in turn have an explicit dependency on the former.

                // Get the relevant types needed.
                Type? socketsHttpHandlerType = Type.GetType("System.Net.Http.SocketsHttpHandler, System.Net.Http", throwOnError: false);
                Type? httpMessageHandlerType = Type.GetType("System.Net.Http.HttpMessageHandler, System.Net.Http", throwOnError: false);
                Type? httpClientType = Type.GetType("System.Net.Http.HttpClient, System.Net.Http", throwOnError: false);
                Type? httpRequestMessageType = Type.GetType("System.Net.Http.HttpRequestMessage, System.Net.Http", throwOnError: false);
                Type? httpResponseMessageType = Type.GetType("System.Net.Http.HttpResponseMessage, System.Net.Http", throwOnError: false);
                Type? httpResponseHeadersType = Type.GetType("System.Net.Http.Headers.HttpResponseHeaders, System.Net.Http", throwOnError: false);
                Type? httpContentType = Type.GetType("System.Net.Http.HttpContent, System.Net.Http", throwOnError: false);
                Type? taskOfHttpResponseMessageType = Type.GetType("System.Threading.Tasks.Task`1[[System.Net.Http.HttpResponseMessage, System.Net.Http]], System.Runtime", throwOnError: false);

                if (socketsHttpHandlerType == null || httpMessageHandlerType == null || httpClientType == null || httpRequestMessageType == null ||
                    httpResponseMessageType == null || httpResponseHeadersType == null || httpContentType == null || taskOfHttpResponseMessageType == null)
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
                MethodInfo? sendAsyncMethod = httpClientType.GetMethod("SendAsync", new Type[] { httpRequestMessageType, typeof(CancellationToken) });
                PropertyInfo? responseContentProp = httpResponseMessageType.GetProperty("Content");
                PropertyInfo? responseStatusCodeProp = httpResponseMessageType.GetProperty("StatusCode");
                PropertyInfo? responseHeadersProp = httpResponseMessageType.GetProperty("Headers");
                PropertyInfo? responseHeadersLocationProp = httpResponseHeadersType.GetProperty("Location");
                MethodInfo? readAsStreamMethod = httpContentType.GetMethod("ReadAsStream", Type.EmptyTypes);
                PropertyInfo? taskOfHttpResponseMessageResultProp = taskOfHttpResponseMessageType.GetProperty("Result");

                if (socketsHttpHandlerCtor == null || pooledConnectionIdleTimeoutProp == null ||
                    allowAutoRedirectProp == null || httpClientCtor == null ||
                    requestUriProp == null || httpRequestMessageCtor == null ||
                    sendMethod == null || sendAsyncMethod == null ||
                    responseContentProp == null || responseStatusCodeProp == null ||
                    responseHeadersProp == null || responseHeadersLocationProp == null ||
                    readAsStreamMethod == null || taskOfHttpResponseMessageResultProp == null)
                {
                    Debug.Fail("Unable to load required members.");
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

                return async (string uriString, CancellationToken cancellationToken, bool async) =>
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
                    object responseMessage;

                    if (async)
                    {
                        Task sendTask = (Task)sendAsyncMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                        await sendTask.ConfigureAwait(false);
                        responseMessage = taskOfHttpResponseMessageResultProp.GetValue(sendTask)!;
                    }
                    else
                    {
                        responseMessage = sendMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                    }

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
                            ReportRedirectsExceeded();

                            return null;
                        }

                        ReportRedirected(redirectUri);

                        // Equivalent of:
                        // requestMessage = new HttpRequestMessage() { RequestUri = redirectUri };
                        // requestMessage.RequestUri = redirectUri;
                        // responseMessage = httpClient.Send(requestMessage, cancellationToken);
                        requestMessage = httpRequestMessageCtor.Invoke(null);
                        requestUriProp.SetValue(requestMessage, redirectUri);

                        if (async)
                        {
                            Task sendTask = (Task)sendAsyncMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                            await sendTask.ConfigureAwait(false);
                            responseMessage = taskOfHttpResponseMessageResultProp.GetValue(sendTask)!;
                        }
                        else
                        {
                            responseMessage = sendMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                        }
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
                    if (async)
                    {
                        await responseStream.CopyToAsync(result).ConfigureAwait(false);
                    }
                    else
                    {
                        responseStream.CopyTo(result);
                    }
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
                ReportRedirectNotFollowed(location);

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
