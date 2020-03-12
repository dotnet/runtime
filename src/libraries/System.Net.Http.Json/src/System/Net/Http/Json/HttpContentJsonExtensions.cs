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
            return ReadFromJsonAsyncCore(content, type, options, cancellationToken);
        }

        public static Task<T> ReadFromJsonAsync<T>(this HttpContent content, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReadFromJsonAsyncCore<T>(content, options, cancellationToken);
        }

        private static async Task<object?> ReadFromJsonAsyncCore(HttpContent content, Type type, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await GetJsonStreamFromContentAsync(content, cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync(contentStream, type, options, cancellationToken);
        }

        private static async Task<T> ReadFromJsonAsyncCore<T>(HttpContent content, JsonSerializerOptions? options, CancellationToken cancellationToken)
        {
            Stream contentStream = await GetJsonStreamFromContentAsync(content, cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(contentStream, options, cancellationToken);
        }

        private static async Task<Stream> GetJsonStreamFromContentAsync(HttpContent content, CancellationToken cancellationToken)
        {
            string? mediaType = content.Headers.ContentType?.MediaType;

            if (mediaType != JsonContent.JsonMediaType &&
                mediaType != MediaTypeNames.Text.Plain)
            {
                throw new NotSupportedException(SR.ContentTypeNotSupported);
            }

            Debug.Assert(content.Headers.ContentType != null);

            string? charset = content.Headers.ContentType.CharSet;
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
            }

            //TODO: We should allow encodings other than UTF-8 and we should transcode to UTF-8 if we get one.
            // This would be easier to achieve once https://github.com/dotnet/runtime/issues/30260 is done.
            if (encoding != null && encoding != Encoding.UTF8)
            {
                throw new NotSupportedException(SR.CharSetNotSupported);
            }

            return await content.ReadAsStreamAsync().ConfigureAwait(false);
        }
    }
}
