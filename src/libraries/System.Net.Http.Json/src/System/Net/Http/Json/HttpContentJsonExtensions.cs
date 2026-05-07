// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        internal const string SerializationUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
        internal const string SerializationDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.";

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="content">The content to read from.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="options">Options to control the behavior during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static async Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return await ReadFromJsonAsyncCore(content, type, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="content">The content to read from.</param>
        /// <param name="type">The type of the object to deserialize to and return.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static async Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, CancellationToken cancellationToken = default)
        {
            return await ReadFromJsonAsync(content, type, options: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="content">The content to read from.</param>
        /// <param name="options">Options to control the behavior during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <typeparam name="T">The target type to deserialize to.</typeparam>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static async Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return await ReadFromJsonAsyncCore<T>(content, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as JSON in an asynchronous operation.
        /// </summary>
        /// <param name="content">The content to read from.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <typeparam name="T">The target type to deserialize to.</typeparam>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static async Task<T?> ReadFromJsonAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
        {
            return await ReadFromJsonAsync<T>(content, options: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, options ?? JsonSerializerOptions.Web, cancellationToken).ConfigureAwait(false);
            }
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        private static async Task<T?> ReadFromJsonAsyncCore<T>(HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? JsonSerializerOptions.Web, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return await ReadFromJsonAsyncCore(content, type, context, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return await ReadFromJsonAsyncCore(content, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T?> ReadFromJsonAsyncCore<T>(HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static ValueTask<Stream> GetContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
        {
            Task<Stream> task = ReadHttpContentStreamAsync(content, cancellationToken);

            return JsonHelpers.GetEncoding(content) is Encoding sourceEncoding && sourceEncoding != Encoding.UTF8
                ? GetTranscodingStreamAsync(task, sourceEncoding)
                : new(task);
        }

        private static async ValueTask<Stream> GetTranscodingStreamAsync(Task<Stream> task, Encoding sourceEncoding)
        {
            Stream contentStream = await task.ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            return GetTranscodingStream(contentStream, sourceEncoding);
        }
    }
}
