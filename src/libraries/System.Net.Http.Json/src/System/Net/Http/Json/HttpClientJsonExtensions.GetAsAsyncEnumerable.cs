// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    /// <summary>
    /// Contains the extensions methods for using JSON as the content-type in HttpClient.
    /// </summary>
    public static partial class HttpClientJsonExtensions
    {
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, CreateUri(requestUri), options, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore<TValue>(s_getAsync, client, requestUri, options, cancellationToken);

        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable(client, CreateUri(requestUri), jsonTypeInfo, cancellationToken);

        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore(s_getAsync, client, requestUri, jsonTypeInfo, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue>(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsyncEnumerable<TValue>(stream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellation), options, cancellationToken);

        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue>(Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod, HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken) =>
            FromJsonStreamAsyncCore(getMethod, client, requestUri, static (stream, options, cancellation) => JsonSerializer.DeserializeAsyncEnumerable(stream, options, cancellation), jsonTypeInfo, cancellationToken);

        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue, TJsonOptions>(
            Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod,
            HttpClient client,
            Uri? requestUri,
            Func<Stream, TJsonOptions, CancellationToken, IAsyncEnumerable<TValue?>> deserializeMethod,
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

            static async IAsyncEnumerable<TValue?> Core(
                HttpClient client,
                Task<HttpResponseMessage> responseTask,
                bool usingResponseHeadersRead,
                CancellationTokenSource? linkedCTS,
                Func<Stream, TJsonOptions, CancellationToken, IAsyncEnumerable<TValue?>> deserializeMethod,
                TJsonOptions jsonOptions,
                [EnumeratorCancellation] CancellationToken cancellationToken)
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

                    using Stream contentStream = await HttpContentJsonExtensions.GetContentStreamAsync(response.Content, linkedCTS?.Token ?? cancellationToken).ConfigureAwait(false);

                    // If ResponseHeadersRead wasn't used, HttpClient will have already buffered the whole response upfront. No need to check the limit again.
                    Stream readStream = usingResponseHeadersRead
                        ? new LengthLimitReadStream(contentStream, (int)client.MaxResponseContentBufferSize)
                        : contentStream;

                    await foreach (TValue value in deserializeMethod(readStream, jsonOptions, linkedCTS?.Token ?? cancellationToken).ConfigureAwait(false))
                    {
                        yield return value;
                    }
                }
                finally
                {
                    linkedCTS?.Dispose();
                }
            }
        }
    }
}
