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
        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, mediaType: null, options);
            return client.PatchAsync(requestUri, content, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Options to control the behavior during serialization. The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, mediaType: null, options);
            return client.PatchAsync(requestUri, content, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, TValue value, CancellationToken cancellationToken)
            => client.PatchAsJsonAsync(requestUri, value, options: null, cancellationToken);

        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, CancellationToken cancellationToken)
            => client.PatchAsJsonAsync(requestUri, value, options: null, cancellationToken);

        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the behavior during serialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, jsonTypeInfo);
            return client.PatchAsync(requestUri, content, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request to the specified Uri containing the <paramref name="value"/> serialized as JSON in the request body.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the behavior during serialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> is null.</exception>
        public static Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(this HttpClient client, Uri? requestUri, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            JsonContent content = JsonContent.Create(value, jsonTypeInfo);
            return client.PatchAsync(requestUri, content, cancellationToken);
        }
    }
}
