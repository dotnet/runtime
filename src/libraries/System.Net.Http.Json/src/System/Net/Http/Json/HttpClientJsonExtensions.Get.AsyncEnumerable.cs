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
        /// <summary>
        /// Sends an <c>HTTP GET</c> request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, CreateUri(requestUri), options, cancellationToken);

        /// <summary>
        /// Sends an <c>HTTP GET</c>request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            Uri? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore<TValue>(client, requestUri, options, cancellationToken);

        /// <summary>
        /// Sends an <c>HTTP GET</c>request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the behavior during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable(client, CreateUri(requestUri), jsonTypeInfo, cancellationToken);

        /// <summary>
        /// Sends an <c>HTTP GET</c>request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the behavior during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            Uri? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default) =>
            FromJsonStreamAsyncCore(client, requestUri, jsonTypeInfo, cancellationToken);

        /// <summary>
        /// Sends an <c>HTTP GET</c>request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> GetFromJsonAsAsyncEnumerable<TValue>(
            this HttpClient client,
            [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
            CancellationToken cancellationToken = default) =>
            GetFromJsonAsAsyncEnumerable<TValue>(client, requestUri, options: null, cancellationToken);

        /// <summary>
        /// Sends an <c>HTTP GET</c>request to the specified <paramref name="requestUri"/> and returns the value that results
        /// from deserializing the response body as JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is <see langword="null"/>.</exception>
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
            HttpClient client,
            Uri? requestUri,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            var jsonTypeInfo = (JsonTypeInfo<TValue>)JsonHelpers.GetJsonTypeInfo(typeof(TValue), options);

            return FromJsonStreamAsyncCore(client, requestUri, jsonTypeInfo, cancellationToken);
        }

        private static IAsyncEnumerable<TValue?> FromJsonStreamAsyncCore<TValue>(
            HttpClient client,
            Uri? requestUri,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return Core(client, requestUri, jsonTypeInfo, cancellationToken);

            static async IAsyncEnumerable<TValue?> Core(
                HttpClient client,
                Uri? requestUri,
                JsonTypeInfo<TValue> jsonTypeInfo,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using Stream readStream = await GetHttpResponseStreamAsync(client, response, false, cancellationToken)
                    .ConfigureAwait(false);

                await foreach (TValue? value in JsonSerializer.DeserializeAsyncEnumerable<TValue>(
                    readStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
        }
    }
}
