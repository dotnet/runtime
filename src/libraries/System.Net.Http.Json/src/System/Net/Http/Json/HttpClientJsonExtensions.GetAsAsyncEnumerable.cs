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
    public static partial class HttpClientJsonExtensions
    {
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, CreateUri(requestUri), options, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            Uri? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore<TValue>(s_getAsync, client, requestUri, options, cancellationToken);

        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable(client, CreateUri(requestUri), jsonTypeInfo, cancellationToken);

        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            Uri? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore(s_getAsync, client, requestUri, jsonTypeInfo, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            Uri? requestUri,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue>(
            Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod,
            HttpClient client,
            Uri? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            CancellationTokenSource? linkedCTS = CreateLinkedCTSFromClientTimeout(client, cancellationToken);
            Task<HttpResponseMessage> responseTask = GetHttpResponseMessageTask(getMethod, client, requestUri, linkedCTS, cancellationToken);

            return Core(client, responseTask, options ?? JsonHelpers.s_defaultSerializerOptions, linkedCTS, cancellationToken);

            static async IAsyncEnumerable<TValue?> Core(
                HttpClient client,
                Task<HttpResponseMessage> responseTask,
                JsonSerializerOptions options,
                CancellationTokenSource? linkedCTS,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                try
                {
                    using HttpResponseMessage response = await EnsureHttpResponseAsync(client, responseTask)
                        .ConfigureAwait(false);

                    await foreach (TValue? value in response.Content.ReadFromJsonAsAsyncEnumerable<TValue>(
                        options, cancellationToken))
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

        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue>(
            Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod,
            HttpClient client,
            Uri? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            CancellationTokenSource? linkedCTS = CreateLinkedCTSFromClientTimeout(client, cancellationToken);
            Task<HttpResponseMessage> responseTask = GetHttpResponseMessageTask(getMethod, client, requestUri, linkedCTS, cancellationToken);

            return Core(client, responseTask, jsonTypeInfo, linkedCTS, cancellationToken);

            static async IAsyncEnumerable<TValue?> Core(
                HttpClient client,
                Task<HttpResponseMessage> responseTask,
                JsonTypeInfo<TValue> jsonTypeInfo,
                CancellationTokenSource? linkedCTS,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                try
                {
                    using HttpResponseMessage response = await EnsureHttpResponseAsync(client, responseTask)
                        .ConfigureAwait(false);

                    await foreach (TValue? value in response.Content.ReadFromJsonAsAsyncEnumerable<TValue>(
                        jsonTypeInfo, cancellationToken))
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

        private static CancellationTokenSource? CreateLinkedCTSFromClientTimeout(
            HttpClient client,
            CancellationToken cancellationToken)
        {
            TimeSpan timeout = client.Timeout;

            // Create the CTS before the initial SendAsync so that the SendAsync counts against the timeout.
            CancellationTokenSource? linkedCTS = null;
            if (timeout != Timeout.InfiniteTimeSpan)
            {
                linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCTS.CancelAfter(timeout);
            }

            return linkedCTS;
        }

        private static Task<HttpResponseMessage> GetHttpResponseMessageTask(
            Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> getMethod,
            HttpClient client,
            Uri? requestUri,
            CancellationTokenSource? linkedCTS,
            CancellationToken cancellationToken)
        {
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

            return responseTask;
        }

        private static async Task<HttpResponseMessage> EnsureHttpResponseAsync(
            HttpClient client,
            Task<HttpResponseMessage> responseTask)
        {
            HttpResponseMessage response = await responseTask.ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            Debug.Assert(client.MaxResponseContentBufferSize is > 0 and <= int.MaxValue);
            int contentLengthLimit = (int)client.MaxResponseContentBufferSize;

            if (response.Content.Headers.ContentLength is long contentLength && contentLength > contentLengthLimit)
            {
                LengthLimitReadStream.ThrowExceededBufferLimit(contentLengthLimit);
            }

            return response;
        }
    }
}
