// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, mediaType: null, options);
            return PatchAsync(client, requestUri, content, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, mediaType: null, options);
            return PatchAsync(client, requestUri, content, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, CancellationToken cancellationToken)
            => client.PatchAsJsonAsync(requestUri, value, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
            => client.PatchAsJsonAsync(requestUri, value, options: null, cancellationToken);

        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent<TValue> content = new(value, jsonTypeInfo);
            return PatchAsync(client, requestUri, content, cancellationToken);
        }

        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent<TValue> content = new(value, jsonTypeInfo);
            return PatchAsync(client, requestUri, content, cancellationToken);
        }

#if NETCOREAPP
        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return client.PatchAsync(requestUri, content, cancellationToken);
        }

        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return client.PatchAsync(requestUri, content, cancellationToken);
        }
#else
        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            Uri? uri = string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute);
            return PatchAsync(client, uri, content, cancellationToken);
        }

        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            // HttpClient.PatchAsync is not available in .NET standard and NET462
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
            return client.SendAsync(request, cancellationToken);
        }
#endif
    }
}
