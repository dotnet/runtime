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
        private static readonly Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> s_getAsync =
            static async (client, uri, cancellation) => await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync(client, CreateUri(requestUri), type, options, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            await FromJsonAsyncCore(static async (client, uri, cancellation) => await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false), client, requestUri, type, options, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync<TValue>(client, CreateUri(requestUri), options, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            await FromJsonAsyncCore<TValue>(s_getAsync, client, requestUri, options, cancellationToken).ConfigureAwait(false);

        public static async Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync(client, CreateUri(requestUri), type, context, cancellationToken).ConfigureAwait(false);

        public static async Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default) =>
            await FromJsonAsyncCore(s_getAsync, client, requestUri, type, context, cancellationToken).ConfigureAwait(false);

        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync(client, CreateUri(requestUri), jsonTypeInfo, cancellationToken).ConfigureAwait(false);

        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            await FromJsonAsyncCore(s_getAsync, client, requestUri, jsonTypeInfo, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<object?> GetFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync(client, requestUri, type, options: null, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<object?> GetFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync(client, requestUri, type, options: null, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync<TValue>(client, requestUri, options: null, cancellationToken).ConfigureAwait(false);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static async Task<TValue?> GetFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default) =>
            await GetFromJsonAsync<TValue>(client, requestUri, options: null, cancellationToken).ConfigureAwait(false);
    }
}
