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
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore(taskResponse, type, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore<TValue>(taskResponse, options, cancellationToken);
        }

        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore(taskResponse, type, context, cancellationToken);
        }

        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore(taskResponse, jsonTypeInfo, cancellationToken);
        }

        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Task<HttpResponseMessage> taskResponse = client.DeleteAsync(requestUri, cancellationToken);
            return DeleteFromJsonAsyncCore<TValue>(taskResponse, jsonTypeInfo, cancellationToken);
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax("Uri")] string? requestUri, Type type, CancellationToken cancellationToken = default)
            => client.DeleteFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default)
            => client.DeleteFromJsonAsync(requestUri, type, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, CancellationToken cancellationToken = default)
            => client.DeleteFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default)
            => client.DeleteFromJsonAsync<TValue>(requestUri, options: null, cancellationToken);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private static async Task<object?> DeleteFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // DeleteAsync will usually return Content as not-null.
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
        private static async Task<T?> DeleteFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                // Nullable forgiving reason:
                // DeleteAsync will usually return Content as not-null.
                // If Content happens to be null, the extension will throw.
                return await ReadFromJsonAsyncHelper<T>(response.Content!, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<object?> DeleteFromJsonAsyncCore(Task<HttpResponseMessage> taskResponse, Type type, JsonSerializerContext context, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content!.ReadFromJsonAsync(type, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T?> DeleteFromJsonAsyncCore<T>(Task<HttpResponseMessage> taskResponse, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await taskResponse.ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content!.ReadFromJsonAsync<T>(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
