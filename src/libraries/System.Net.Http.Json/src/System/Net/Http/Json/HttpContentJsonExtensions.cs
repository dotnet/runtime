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

[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Target = "M:System.Net.Http.Json.HttpContentJsonExtensions.<ReadFromJsonAsyncCore>d__3.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern",
    Target = "M:System.Net.Http.Json.HttpContentJsonExtensions.<ReadFromJsonAsyncCore>d__3.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Target = "M:System.Net.Http.Json.HttpContentJsonExtensions.<ReadFromJsonAsyncCore>d__4`1.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern",
    Target = "M:System.Net.Http.Json.HttpContentJsonExtensions.<ReadFromJsonAsyncCore>d__4`1.MoveNext()",
    Scope = "member",
    Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        internal const string SerializationUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static Task<object?> ReadFromJsonAsync(this HttpContent content, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Encoding? sourceEncoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);

            return ReadFromJsonAsyncCore(content, type, sourceEncoding, options, cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static Task<T?> ReadFromJsonAsync<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Encoding? sourceEncoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);

            return ReadFromJsonAsyncCore<T>(content, sourceEncoding, options, cancellationToken);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, [DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] Type type, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, options ?? JsonContent.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static async Task<T?> ReadFromJsonAsyncCore<[DynamicallyAccessedMembers(JsonHelpers.DeserializationMemberTypes)] T>(HttpContent content, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? JsonContent.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerContext context, CancellationToken cancellationToken = default)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Encoding? sourceEncoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);

            return ReadFromJsonAsyncCore(content, type, sourceEncoding, context, cancellationToken);
        }

        public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Encoding? sourceEncoding = JsonHelpers.GetEncoding(content.Headers.ContentType?.CharSet);

            return ReadFromJsonAsyncCore<T>(content, sourceEncoding, jsonTypeInfo, cancellationToken);
        }

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, Encoding? sourceEncoding, JsonSerializerContext context, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T?> ReadFromJsonAsyncCore<T>(HttpContent content, Encoding? sourceEncoding, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetContentStream(content, sourceEncoding, cancellationToken).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<Stream> GetContentStream(HttpContent content, Encoding? sourceEncoding, CancellationToken cancellationToken)
        {
            Stream contentStream = await ReadHttpContentStreamAsync(content, cancellationToken).ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                contentStream = GetTranscodingStream(contentStream, sourceEncoding);
            }

            return contentStream;
        }
    }
}
