// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class X509ResourceClient
    {
        private const long DefaultAiaDownloadLimit = 100 * 1024 * 1024;
        private static long AiaDownloadLimit { get; } = GetValue("System.Security.Cryptography.AiaDownloadLimit", DefaultAiaDownloadLimit);

        private static readonly Func<string, CancellationToken, bool, Task<byte[]?>>? s_downloadBytes = CreateDownloadBytesFunc();

        static partial void ReportNoClient();
        static partial void ReportNegativeTimeout();
        static partial void ReportDownloadStart(long totalMillis, string uri);
        static partial void ReportDownloadStop(int bytesDownloaded);
        static partial void ReportRedirectsExceeded();
        static partial void ReportRedirected(Uri newUri);
        static partial void ReportRedirectNotFollowed(Uri redirectUri);

        internal static byte[]? DownloadAsset(string uri, TimeSpan downloadTimeout)
        {
            Task<byte[]?> task = DownloadAssetCore(uri, downloadTimeout, async: false);
            Debug.Assert(task.IsCompletedSuccessfully);
            return task.Result;
        }

        internal static Task<byte[]?> DownloadAssetAsync(string uri, TimeSpan downloadTimeout)
        {
            return DownloadAssetCore(uri, downloadTimeout, async: true);
        }

        private static async Task<byte[]?> DownloadAssetCore(string uri, TimeSpan downloadTimeout, bool async)
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
                Task<byte[]?> task = s_downloadBytes(uri, cts?.Token ?? default, async);
                await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                if (task.IsCompletedSuccessfully)
                {
                    return task.Result;
                }
            }
            catch { }
            finally
            {
                cts?.Dispose();

                ReportDownloadStop(ret?.Length ?? 0);
            }

            return null;
        }

        private const string SocketsHttpHandlerTypeName = "System.Net.Http.SocketsHttpHandler, System.Net.Http";
        private const string HttpMessageHandlerTypeName = "System.Net.Http.HttpMessageHandler, System.Net.Http";
        private const string HttpClientTypeName = "System.Net.Http.HttpClient, System.Net.Http";
        private const string HttpRequestMessageTypeName = "System.Net.Http.HttpRequestMessage, System.Net.Http";
        private const string HttpResponseMessageTypeName = "System.Net.Http.HttpResponseMessage, System.Net.Http";
        private const string HttpResponseHeadersTypeName = "System.Net.Http.Headers.HttpResponseHeaders, System.Net.Http";
        private const string HttpContentTypeName = "System.Net.Http.HttpContent, System.Net.Http";
        private const string TaskOfHttpResponseMessageTypeName = "System.Threading.Tasks.Task`1[[System.Net.Http.HttpResponseMessage, System.Net.Http]], System.Runtime";

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(SocketsHttpHandlerTypeName)]
        private static extern object CreateHttpHandler();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PooledConnectionIdleTimeout")]
        private static extern TimeSpan GetPooledConnectionIdleTimeout([UnsafeAccessorType(SocketsHttpHandlerTypeName)] object handler);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AllowAutoRedirect")]
        private static extern bool GetAllowAutoRedirect([UnsafeAccessorType(SocketsHttpHandlerTypeName)] object handler);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AllowAutoRedirect")]
        private static extern void SetAllowAutoRedirect([UnsafeAccessorType(SocketsHttpHandlerTypeName)] object handler, bool value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_PooledConnectionIdleTimeout")]
        private static extern void SetPooledConnectionIdleTimeout([UnsafeAccessorType(SocketsHttpHandlerTypeName)] object handler, TimeSpan value);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(HttpClientTypeName)]
        private static extern object CreateHttpClient([UnsafeAccessorType(HttpMessageHandlerTypeName)] object handler);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxResponseContentBufferSize")]
        private static extern long GetMaxResponseContentBufferSize([UnsafeAccessorType(HttpClientTypeName)] object client);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxResponseContentBufferSize")]
        private static extern void SetMaxResponseContentBufferSize([UnsafeAccessorType(HttpClientTypeName)] object client, long value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_RequestUri")]
        private static extern Uri? GetRequestUri([UnsafeAccessorType(HttpRequestMessageTypeName)] object requestMessage);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_RequestUri")]
        private static extern void SetRequestUri([UnsafeAccessorType(HttpRequestMessageTypeName)] object requestMessage, Uri? value);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType(HttpRequestMessageTypeName)]
        private static extern object CreateRequestMessage();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Send")]
        [return: UnsafeAccessorType(HttpResponseMessageTypeName)]
        private static extern object Send([UnsafeAccessorType(HttpClientTypeName)] object client, [UnsafeAccessorType(HttpRequestMessageTypeName)] object requestMessage, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SendAsync")]
        [return: UnsafeAccessorType(TaskOfHttpResponseMessageTypeName)]
        private static extern object SendAsync([UnsafeAccessorType(HttpClientTypeName)] object client, [UnsafeAccessorType(HttpRequestMessageTypeName)] object requestMessage, CancellationToken cancellationToken);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Content")]
        [return: UnsafeAccessorType(HttpContentTypeName)]
        private static extern object GetContent([UnsafeAccessorType(HttpResponseMessageTypeName)] object responseMessage);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_StatusCode")]
        private static extern HttpStatusCode GetStatusCode([UnsafeAccessorType(HttpResponseMessageTypeName)] object responseMessage);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Headers")]
        [return: UnsafeAccessorType(HttpResponseHeadersTypeName)]
        private static extern object GetHeaders([UnsafeAccessorType(HttpResponseMessageTypeName)] object responseMessage);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Location")]
        private static extern Uri? GetLocation([UnsafeAccessorType(HttpResponseHeadersTypeName)] object headers);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReadAsStream")]
        private static extern Stream ReadAsStream([UnsafeAccessorType(HttpContentTypeName)] object content);

        private static Func<string, CancellationToken, bool, Task<byte[]?>>? CreateDownloadBytesFunc()
        {
            try
            {
                // Use reflection to access System.Net.Http:
                // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                // the latter can't in turn have an explicit dependency on the former.

                // Get the relevant types needed.
                Type? taskOfHttpResponseMessageType = Type.GetType(TaskOfHttpResponseMessageTypeName, throwOnError: false);

                PropertyInfo? taskResultProperty = taskOfHttpResponseMessageType?.GetProperty("Result");

                if (taskResultProperty == null)
                {
                    Debug.Fail("Unable to load required type.");
                    return null;
                }

                // Only keep idle connections around briefly, as a compromise between resource leakage and port exhaustion.
                const int PooledConnectionIdleTimeoutSeconds = 15;
                const int MaxRedirections = 10;

                // Use UnsafeAccessors for construction and property access
                object socketsHttpHandler = CreateHttpHandler();
                SetAllowAutoRedirect(socketsHttpHandler, false);
                SetPooledConnectionIdleTimeout(socketsHttpHandler, TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds));

                object httpClient = CreateHttpClient(socketsHttpHandler);
                SetMaxResponseContentBufferSize(httpClient, AiaDownloadLimit);

                return async (string uriString, CancellationToken cancellationToken, bool async) =>
                {
                    Uri uri = new Uri(uriString);

                    if (!IsAllowedScheme(uri.Scheme))
                    {
                        return null;
                    }

                    object requestMessage = CreateRequestMessage();
                    SetRequestUri(requestMessage, uri);
                    object responseMessage;

                    if (async)
                    {
                        Task sendTask = (Task)SendAsync(httpClient, requestMessage, cancellationToken);
                        await sendTask.ConfigureAwait(false);
                        responseMessage = taskResultProperty.GetValue(sendTask)!;
                    }
                    else
                    {
                        responseMessage = Send(httpClient, requestMessage, cancellationToken);
                    }

                    int redirections = 0;
                    Uri? redirectUri;
                    bool hasRedirect;
                    while (true)
                    {
                        int statusCode = (int)GetStatusCode(responseMessage);
                        object responseHeaders = GetHeaders(responseMessage);
                        Uri? location = GetLocation(responseHeaders);
                        redirectUri = GetUriForRedirect(GetRequestUri(requestMessage)!, statusCode, location, out hasRedirect);
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

                        requestMessage = CreateRequestMessage();
                        SetRequestUri(requestMessage, redirectUri);

                        if (async)
                        {
                            Task sendTask = (Task)SendAsync(httpClient, requestMessage, cancellationToken);
                            await sendTask.ConfigureAwait(false);
                            responseMessage = taskResultProperty.GetValue(sendTask)!;
                        }
                        else
                        {
                            responseMessage = Send(httpClient, requestMessage, cancellationToken);
                        }
                    }

                    if (hasRedirect && redirectUri == null)
                    {
                        return null;
                    }

                    object content = GetContent(responseMessage);
                    using Stream responseStream = ReadAsStream(content);

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

        private static long GetValue(string name, long defaultValue)
        {
            object? data = AppContext.GetData(name);
            if (data is null)
            {
                return defaultValue;
            }
            try
            {
                return Convert.ToInt64(data);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
