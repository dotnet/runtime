// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        public static Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient client, string requestUri, Type type, object? value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = CreateJsonContent(type, value, options);
            return client.PutAsync(requestUri, content, cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient client, Uri requestUri, Type type, object? value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = CreateJsonContent(type, value, options);
            return client.PutAsync(requestUri, content, cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = CreateJsonContent<T>(value, options);
            return client.PutAsync(requestUri, content, cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = CreateJsonContent<T>(value, options);
            return client.PutAsync(requestUri, content, cancellationToken);
        }

        private static JsonContent CreateJsonContent(Type type, object? value, JsonSerializerOptions? options)
        {
            return new JsonContent(type, value, options);
        }

        private static JsonContent CreateJsonContent<T>(T value, JsonSerializerOptions? options)
        {
            return JsonContent.Create<T>(value, options);
        }
    }
}
