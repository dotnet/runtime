// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ValidateContent(content);
            Debug.Assert(content.Headers.ContentType != null);
            Encoding? sourceEncoding = JsonContent.GetEncoding(content.Headers.ContentType.CharSet);

            return ReadFromJsonAsyncCore(content, type, sourceEncoding, options, cancellationToken);
        }

        public static Task<T> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ValidateContent(content);
            Debug.Assert(content.Headers.ContentType != null);
            Encoding? sourceEncoding = JsonContent.GetEncoding(content.Headers.ContentType.CharSet);

            return ReadFromJsonAsyncCore<T>(content, sourceEncoding, options, cancellationToken);
        }

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await ReadHttpContentStreamAsync(content, cancellationToken).ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                contentStream = GetTranscodingStream(contentStream, sourceEncoding);
            }

            using (contentStream)
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, options ?? JsonContent.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await ReadHttpContentStreamAsync(content, cancellationToken).ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                contentStream = GetTranscodingStream(contentStream, sourceEncoding);
            }

            using (contentStream)
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, options ?? JsonContent.s_defaultSerializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void ValidateContent(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string? mediaType = content.Headers.ContentType?.MediaType;

            if (mediaType == null ||
                !mediaType.Equals(JsonContent.JsonMediaType, StringComparison.OrdinalIgnoreCase) &&
                !IsValidStructuredSyntaxJsonSuffix(mediaType.AsSpan()))
            {
                throw new NotSupportedException(SR.Format(SR.ContentTypeNotSupported, mediaType));
            }
        }

        private static bool IsValidStructuredSyntaxJsonSuffix(ReadOnlySpan<char> mediaType)
        {
            int index = 0;
            int typeLength = mediaType.IndexOf('/');

            ReadOnlySpan<char> type = mediaType.Slice(index, typeLength);
            if (typeLength < 0 ||
                type.CompareTo(JsonContent.JsonType.AsSpan(), StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            index += typeLength + 1;
            int suffixStart = mediaType.Slice(index).IndexOf('+');

            // Empty prefix subtype ("application/+json") not allowed.
            if (suffixStart <= 0)
            {
                return false;
            }

            index += suffixStart + 1;
            ReadOnlySpan<char> suffix = mediaType.Slice(index);
            if (suffix.CompareTo(JsonContent.JsonSubtype.AsSpan(), StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            return true;
        }
    }
}
