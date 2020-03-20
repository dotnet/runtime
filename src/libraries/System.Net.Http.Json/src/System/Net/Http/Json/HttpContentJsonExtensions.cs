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
        {
            ValidateContent(content);
            Debug.Assert(content.Headers.ContentType != null);
            Encoding? sourceEncoding = GetEncoding(content.Headers.ContentType.CharSet);

            return ReadFromJsonAsyncCore(content, type, sourceEncoding, options, cancellationToken);
        }

        public static Task<T> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ValidateContent(content);
            Debug.Assert(content.Headers.ContentType != null);
            Encoding? sourceEncoding = GetEncoding(content.Headers.ContentType.CharSet);

            return ReadFromJsonAsyncCore<T>(content, sourceEncoding, options, cancellationToken);
        }

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                using (Stream transcodingStream = new TranscodingReadStream(contentStream, sourceEncoding))
                {
                    return await JsonSerializer.DeserializeAsync(transcodingStream, type, options, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using (contentStream)
                {
                    return await JsonSerializer.DeserializeAsync(contentStream, type, options, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, Encoding? sourceEncoding, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
            if (sourceEncoding != null && sourceEncoding != Encoding.UTF8)
            {
                using (Stream transcodingStream = new TranscodingReadStream(contentStream, sourceEncoding))
                {
                    return await JsonSerializer.DeserializeAsync<T>(transcodingStream, options, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using (contentStream)
                {
                    return await JsonSerializer.DeserializeAsync<T>(contentStream, options, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static void ValidateContent(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string? mediaType = content.Headers.ContentType?.MediaType;

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
