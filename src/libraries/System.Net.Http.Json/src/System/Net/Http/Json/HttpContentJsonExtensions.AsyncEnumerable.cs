// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as
        /// JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(
            this HttpContent content,
            CancellationToken cancellationToken = default) =>
            ReadFromJsonAsAsyncEnumerable<TValue>(content, options: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as
        /// JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="content">The content to read from.</param>
        /// <param name="options">Options to control the behavior during deserialization.
        /// The default options are those specified by <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(
            this HttpContent content,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return ReadFromJsonAsAsyncEnumerableCore<TValue>(content, options, cancellationToken);
        }

        /// <summary>
        /// Reads the HTTP content and returns the value that results from deserializing the content as
        /// JSON in an async enumerable operation.
        /// </summary>
        /// <typeparam name="TValue">The target type to deserialize to.</typeparam>
        /// <param name="content">The content to read from.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the deserialization behavior.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}"/> that represents the deserialized response body.</returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="content"/> is <see langword="null"/>.
        /// </exception>
        public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(
            this HttpContent content,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return ReadFromJsonAsAsyncEnumerableCore(content, jsonTypeInfo, cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        private static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerableCore<TValue>(
            HttpContent content,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            var jsonTypeInfo = (JsonTypeInfo<TValue>)JsonHelpers.GetJsonTypeInfo(typeof(TValue), options);
            return ReadFromJsonAsAsyncEnumerableCore(content, jsonTypeInfo, cancellationToken);
        }

        private static async IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerableCore<TValue>(
            HttpContent content,
            JsonTypeInfo<TValue> jsonTypeInfo,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using Stream contentStream = await GetContentStreamAsync(content, cancellationToken)
                .ConfigureAwait(false);

            await foreach (TValue? value in JsonSerializer.DeserializeAsyncEnumerable<TValue>(
                contentStream, jsonTypeInfo, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return value;
            }
        }
    }
}
