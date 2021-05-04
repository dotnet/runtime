// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Target = "M:System.Net.Http.Json.HttpClientJsonExtensions.<GetFromJsonAsyncCore>d__12.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern",
    Target = "M:System.Net.Http.Json.HttpClientJsonExtensions.<GetFromJsonAsyncCore>d__12.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Target = "M:System.Net.Http.Json.HttpClientJsonExtensions.<GetFromJsonAsyncCore>d__13`1.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern",
    Target = "M:System.Net.Http.Json.HttpClientJsonExtensions.<GetFromJsonAsyncCore>d__13`1.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]

namespace System.Net.Http.Json
{
    /// <summary>
    /// Contains the extensions methods for using JSON as the content-type in HttpClient.
    /// </summary>
    public static partial class HttpClientJsonExtensions
    {
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] TValue>(this HttpClient client, string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore(taskResponse, jsonTypeInfo, cancellationToken);
        }

        public static Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return GetFromJsonAsyncCore<TValue>(taskResponse, jsonTypeInfo, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, string? requestUri, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] TValue>(this HttpClient client, string? requestUri, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> GetFromJsonAsync<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default)
            => client.GetFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private static async Task<object?> GetFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // GetAsync will usually return Content as not-null.
                // If Content happens to be null, the extension will throw.
                return await response.Content!.ReadFromJsonAsync(type, options, cancellationToken).ConfigureAwait(false);
            }
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private static async Task<T?> GetFromJsonAsyncCore<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] T>(Task<HttpResponseMessage> taskResponse, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // GetAsync will usually return Content as not-null.
                // If Content happens to be null, the extension will throw.
                return await response.Content!.ReadFromJsonAsync<T>(options, cancellationToken).ConfigureAwait(false);
            }
        }

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
