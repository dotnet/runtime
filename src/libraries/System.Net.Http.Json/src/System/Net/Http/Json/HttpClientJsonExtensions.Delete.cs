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
        private static readonly Func<HttpClient, Uri?, CancellationToken, Task<HttpResponseMessage>> s_deleteAsync =
            static (client, uri, cancellation) => client.DeleteAsync(uri, cancellation);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync(client, CreateUri(requestUri), type, options, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(s_deleteAsync, client, requestUri, type, options, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync<TValue>(client, CreateUri(requestUri), options, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonSerializerOptions? options, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore<TValue>(s_deleteAsync, client, requestUri, options, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="context">The JsonSerializerContext used to control the deserialization behavior.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync(client, CreateUri(requestUri), type, context, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="context">The JsonSerializerContext used to control the deserialization behavior.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(s_deleteAsync, client, requestUri, type, context, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the deserialization behavior.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync(client, CreateUri(requestUri), jsonTypeInfo, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the deserialization behavior.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default) =>
            FromJsonAsyncCore(s_deleteAsync, client, requestUri, jsonTypeInfo, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Type type, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync(client, requestUri, type, options: null, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<object?> DeleteFromJsonAsync(this HttpClient client, Uri? requestUri, Type type, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync(client, requestUri, type, options: null, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync<TValue>(client, requestUri, options: null, cancellationToken);

        /// <summary>
        /// Sends a DELETE request to the specified Uri and returns the value that results from deserializing the response body as JSON in an asynchronous operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<TValue?> DeleteFromJsonAsync<TValue>(this HttpClient client, Uri? requestUri, CancellationToken cancellationToken = default) =>
            DeleteFromJsonAsync<TValue>(client, requestUri, options: null, cancellationToken);
    }
}
