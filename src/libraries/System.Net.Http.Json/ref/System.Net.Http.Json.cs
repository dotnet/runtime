// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        public static System.Threading.Tasks.Task<object?> GetFromJsonAsync(this System.Net.Http.HttpClient client, string requestUri, System.Type type, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<object?> GetFromJsonAsync(this System.Net.Http.HttpClient client, System.Uri requestUri, System.Type type, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<T> GetFromJsonAsync<T>(this System.Net.Http.HttpClient client, string requestUri, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<T> GetFromJsonAsync<T>(this System.Net.Http.HttpClient client, System.Uri requestUri, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsJsonAsync(this System.Net.Http.HttpClient client, string requestUri, System.Type type, object value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsJsonAsync(this System.Net.Http.HttpClient client, System.Uri requestUri, System.Type type, object? value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsJsonAsync<T>(this System.Net.Http.HttpClient client, string requestUri, T value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PostAsJsonAsync<T>(this System.Net.Http.HttpClient client, System.Uri requestUri, T value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsJsonAsync(this System.Net.Http.HttpClient client, string requestUri, System.Type type, object? value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsJsonAsync(this System.Net.Http.HttpClient client, System.Uri requestUri, System.Type type, object? value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsJsonAsync<T>(this System.Net.Http.HttpClient client, string requestUri, T value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> PutAsJsonAsync<T>(this System.Net.Http.HttpClient client, System.Uri requestUri, T value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public static partial class HttpContentJsonExtensions
    {
        public static System.Threading.Tasks.Task<object> ReadFromJsonAsync(this System.Net.Http.HttpContent content, System.Type type, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<T> ReadFromJsonAsync<T>(this System.Net.Http.HttpContent content, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class JsonContent : System.Net.Http.HttpContent
    {
        public JsonContent(System.Type type, object? value, System.Net.Http.Headers.MediaTypeHeaderValue mediaType, System.Text.Json.JsonSerializerOptions options = null) { }
        public JsonContent(System.Type type, object? value, string mediaType, System.Text.Json.JsonSerializerOptions options = null) { }
        public JsonContent(System.Type type, object? value, System.Text.Json.JsonSerializerOptions options = null) { }
        public System.Type ObjectType { get { throw null; } }
        public object? Value { get { throw null; } }
        public static System.Net.Http.Json.JsonContent Create<T>(T value, System.Net.Http.Headers.MediaTypeHeaderValue mediaType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static System.Net.Http.Json.JsonContent Create<T>(T value, string mediaType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static System.Net.Http.Json.JsonContent Create<T>(T value, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) { throw null; }
        protected override bool TryComputeLength(out long length) { throw null; }
    }
}
