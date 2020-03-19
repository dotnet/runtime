// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static class HttpContentJsonExtensions
    {
        public static Task<object?> ReadFromJsonAsync(this HttpContent content, Type type, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
            => ReadFromJsonAsyncCore(content, type, options, cancellationToken);

        public static Task<T> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
            => ReadFromJsonAsyncCore<T>(content, options, cancellationToken);

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetJsonStreamFromContentAsync(content).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync(contentStream, type, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            using (Stream contentStream = await GetJsonStreamFromContentAsync(content).ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(contentStream, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<Stream> GetJsonStreamFromContentAsync(HttpContent content)
        {
            ValidateMediaType(content.Headers.ContentType?.MediaType);
            Debug.Assert(content.Headers.ContentType != null);

            Encoding? sourceEncoding = GetEncoding(content.Headers.ContentType.CharSet);

            Stream jsonStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                jsonStream = new TranscodingReadStream(jsonStream, sourceEncoding);
            }

            return jsonStream;
        }

        private static void ValidateMediaType(string? mediaType)
        {
            if (mediaType != JsonContent.JsonMediaType &&
                mediaType != MediaTypeNames.Text.Plain)
            {
                throw new NotSupportedException(SR.ContentTypeNotSupported);
            }
        }

        private static Encoding? GetEncoding(string? charset)
        {
            Encoding? encoding = null;

            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
                    {
                        encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException(SR.CharSetInvalid, e);
                }

                Debug.Assert(encoding != null);
            }

            return encoding;
        }
    }
}
