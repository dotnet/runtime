// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static class HttpClientJsonExtensions
    {
        public static Task<object?> GetFromJsonAsync(
            this HttpClient client,
            string requestUri,
            Type type,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null;
        }

        public static Task<object?> GetFromJsonAsync(
            this HttpClient client,
            Uri requestUri,
            Type type,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null;
        }

        public static Task<T> GetFromJsonAsync<T>(
            this HttpClient client,
            string requestUri,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null;
        }

        public static Task<T> GetFromJsonAsync<T>(
            this HttpClient client,
            Uri requestUri,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null;
        }

        public static Task<HttpResponseMessage> PostAsJsonAsync(
            this HttpClient client,
            string requestUri,
            Type type,
            object? value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null;
        }
    }

    public static class HttpContentJsonExtensions { }

    public class JsonContent : HttpContent
    {
        public Type ObjectType { get; }
        public object? Value { get; }

        public static JsonContent Create<T>(T value, JsonSerializerOptions options = null) { throw null; }

        public static JsonContent Create<T>(T value, MediaTypeHeaderValue mediaType, JsonSerializerOptions options = null) { throw null; }

        public static JsonContent Create<T>(T value, string mediaType, JsonSerializerOptions options = null) { throw null; }

        public JsonContent(Type type, object? value, JsonSerializerOptions options = null) { throw null; }

        public JsonContent(Type type, object? value, MediaTypeHeaderValue mediaType, JsonSerializerOptions options = null) { throw null; }

        public JsonContent(Type type, object? value, string mediaType, JsonSerializerOptions options = null) { throw null; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) { throw null; }

        protected override bool TryComputeLength(out long length) { throw null; }
    }
}
