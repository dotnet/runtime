// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public static Task<object?> GetFromJsonAsync(this HttpClient client!!, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client!!, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client!!, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client!!, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        public static Task<object?> GetFromJsonAsync(this HttpClient client!!, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<object?> GetFromJsonAsync(this HttpClient client!!, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client!!, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, jsonTypeInfo, cancellationToken);
        }

        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client!!, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, jsonTypeInfo, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private static async Task<object?> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // GetAsync will usually return Content as not-null.
                // If Content happens to be null, the extension will throw.
                return await ReadFromJsonAsyncHelper(response.Content!, type, options, cancellationToken).ConfigureAwait(false);
            }

            // Workaround for https://github.com/mono/linker/issues/1416, extracting the offending call into a separate method
            // which can be annotated with suppressions.
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
                Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
            static Task<object?> ReadFromJsonAsyncHelper(HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
                => content.ReadFromJsonAsync(type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private static async Task<T?> GetFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // GetAsync will usually return Content as not-null.
                // If Content happens to be null, the extension will throw.
                return await ReadFromJsonAsyncHelper<T>(response.Content!, options, cancellationToken).ConfigureAwait(false);
            }
        }

        // Workaround for https://github.com/mono/linker/issues/1416, extracting the offending call into a separate method
        // which can be annotated with suppressions.
        // Note that in this case it can't be a local function since that inherits a generic parameter from the parent method
        // which causes a trimmer warning coming from compiler generated code, which is very hard to suppress.
        // Avoid that by declaring it a normal method which fully defines its own generic parameters.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern",
            Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
        private static Task<T?> ReadFromJsonAsyncHelper<T>(HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken)
            => content.ReadFromJsonAsync<T>(options, cancellationToken);

        private static async Task<object?> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerContext context, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content!.ReadFromJsonAsync(type, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T?> GetFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content!.ReadFromJsonAsync<T>(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
