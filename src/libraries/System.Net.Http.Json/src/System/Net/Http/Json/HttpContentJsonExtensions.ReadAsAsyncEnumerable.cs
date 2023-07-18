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
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<T?> ReadFromJsonAsAsyncEnumerable<T>(this HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return ReadFromJsonAsAsyncEnumerableCore<T>(content, options, cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<T?> ReadFromJsonAsAsyncEnumerable<T>(this HttpContent content, CancellationToken cancellationToken = default)
        {
            return ReadFromJsonAsAsyncEnumerable<T>(content, options: null, cancellationToken: cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        private static async IAsyncEnumerable<T?> ReadFromJsonAsAsyncEnumerableCore<T>(HttpContent content, JsonSerializerOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                await foreach (T value in JsonSerializer.DeserializeAsyncEnumerable<T>(contentStream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
        }

        public static IAsyncEnumerable<T?> ReadFromJsonAsAsyncEnumerable<T>(this HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return ReadFromJsonAsAsyncEnumerableCore(content, jsonTypeInfo, cancellationToken);
        }

        private static async IAsyncEnumerable<T?> ReadFromJsonAsAsyncEnumerableCore<T>(HttpContent content, JsonTypeInfo<T> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken).ConfigureAwait(false))
            {
                await foreach (T value in JsonSerializer.DeserializeAsyncEnumerable(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
        }
    }
}
