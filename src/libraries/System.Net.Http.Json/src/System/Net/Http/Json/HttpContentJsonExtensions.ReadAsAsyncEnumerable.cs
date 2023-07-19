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

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        public static IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerable<TValue>(
            this HttpContent content,
            CancellationToken cancellationToken = default)
        {
            return ReadFromJsonAsAsyncEnumerable<TValue>(content, options: null, cancellationToken: cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationDynamicCodeMessage)]
        private static async IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerableCore<TValue>(
            HttpContent content,
            JsonSerializerOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken)
                .ConfigureAwait(false))
            {
                await foreach (TValue? value in JsonSerializer.DeserializeAsyncEnumerable<TValue>(
                    contentStream, options ?? JsonHelpers.s_defaultSerializerOptions, cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return value;
                }
            }
        }

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

        private static async IAsyncEnumerable<TValue?> ReadFromJsonAsAsyncEnumerableCore<TValue>(
            HttpContent content,
            JsonTypeInfo<TValue> jsonTypeInfo,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStreamAsync(content, cancellationToken)
                .ConfigureAwait(false))
            {
                await foreach (TValue? value in JsonSerializer.DeserializeAsyncEnumerable<TValue>(
                    contentStream, jsonTypeInfo, cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return value;
                }
            }
        }
    }
}
