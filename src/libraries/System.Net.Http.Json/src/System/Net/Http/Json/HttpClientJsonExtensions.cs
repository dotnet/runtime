// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        private static Task<object?> FromJsonAsyncCore(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsync(stream, options.type, options.options ?? JsonHelpers.s_defaultSerializerOptions, cancellation), (type, options), cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        private static Task<TValue?> FromJsonAsyncCore<TValue>(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsync<TValue>(stream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellation), options, cancellationToken);

        private static Task<object?> FromJsonAsyncCore(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsync(stream, options.type, options.context, cancellation), (type, context), cancellationToken);

        private static Task<TValue?> FromJsonAsyncCore<TValue>(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken) =>
            FromJsonAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsync(stream, options, cancellation), jsonTypeInfo, cancellationToken);

        private static Task<TValue?> FromJsonAsyncCore<TValue, TJsonOptions>(
            Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod,
            HttpClient client,
            Uri? requestUri,
            Func<Stream, TJsonOptions, CancellationToken, ValueTask<TValue?>> deserializeMethod,
            TJsonOptions jsonOptions,
            CancellationToken cancellationToken)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            TimeSpan timeout = client.Timeout;

            // Create the CTS before the initial SendAsync so that the SendAsync counts against the timeout.
            CancellationTokenSource? linkedCTS = null;
            if (timeout != Timeout.InfiniteTimeSpan)
            {
                linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCTS.CancelAfter(timeout);
            }

            // We call SendAsync outside of the async Core method to propagate exception even without awaiting the returned task.
            Task<HttpResponseMessage> responseTask;
            try
            {
                // Intentionally using cancellationToken instead of the linked one here as HttpClient will enforce the Timeout on its own for this part
                responseTask = getMethod(client, requestUri, cancellationToken);
            }
            catch
            {
                linkedCTS?.Dispose();
                throw;
            }

            bool usingResponseHeadersRead = !ReferenceEquals(getMethod, s_deleteAsync);

            return Core(client, responseTask, usingResponseHeadersRead, linkedCTS, deserializeMethod, jsonOptions, cancellationToken);

            static async Task<TValue?> Core(
                HttpClient client,
                Task<HttpResponseMessage> responseTask,
                bool usingResponseHeadersRead,
                CancellationTokenSource? linkedCTS,
                Func<Stream, TJsonOptions, CancellationToken, ValueTask<TValue?>> deserializeMethod,
                TJsonOptions jsonOptions,
                CancellationToken cancellationToken)
            {
                try
                {
                    using HttpResponseMessage response = await responseTask.ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    Debug.Assert(client.MaxResponseContentBufferSize is > 0 and <= int.MaxValue);
                    int contentLengthLimit = (int)client.MaxResponseContentBufferSize;

                    if (response.Content.Headers.ContentLength is long contentLength && contentLength > contentLengthLimit)
                    {
                        LengthLimitReadStream.ThrowExceededBufferLimit(contentLengthLimit);
                    }

                    try
                    {
                        using Stream contentStream = await HttpContentJsonExtensions.GetContentStreamAsync(response.Content, linkedCTS?.Token ?? cancellationToken).ConfigureAwait(false);

                        // If ResponseHeadersRead wasn't used, HttpClient will have already buffered the whole response upfront. No need to check the limit again.
                        Stream readStream = usingResponseHeadersRead
                            ? new LengthLimitReadStream(contentStream, (int)client.MaxResponseContentBufferSize)
                            : contentStream;

                        return await deserializeMethod(readStream, jsonOptions, linkedCTS?.Token ?? cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce) when ((linkedCTS?.Token.IsCancellationRequested == true) && !cancellationToken.IsCancellationRequested)
                    {
                        // Matches how HttpClient throws a timeout exception.
                        string message = SR.Format(SR.net_http_request_timedout, client.Timeout.TotalSeconds);
#if NETCOREAPP
                        throw new TaskCanceledException(message, new TimeoutException(oce.Message, oce), oce.CancellationToken);
#else
                        throw new TaskCanceledException(message, new TimeoutException(oce.Message, oce));
#endif
                    }
                }
                finally
                {
                    linkedCTS?.Dispose();
                }
            }
        }

        private static Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}
