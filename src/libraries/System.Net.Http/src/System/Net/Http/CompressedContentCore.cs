// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Shared implementation for the per-algorithm compressed content types. Holds the inner content and
    /// drives serialization through a caller-provided factory that wraps the output stream in a compression stream.
    /// </summary>
    internal sealed class CompressedContentCore
    {
        private readonly HttpContent _originalContent;
        private readonly Func<Stream, Stream> _createCompressionStream;
        private bool _contentConsumed;

        public CompressedContentCore(HttpContent originalContent, Func<Stream, Stream> createCompressionStream)
        {
            _originalContent = originalContent;
            _createCompressionStream = createCompressionStream;
        }

        public static void InitializeHeaders(HttpContent target, HttpContent source, string encoding)
        {
            // Copy headers from the original content.
            foreach (KeyValuePair<string, IEnumerable<string>> header in source.Headers)
            {
                target.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Append our encoding to the Content-Encoding header (supports stacking per HTTP spec).
            target.Headers.ContentEncoding.Add(encoding);

            // Remove Content-Length since we don't know the compressed size upfront.
            target.Headers.ContentLength = null;
        }

        public void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ThrowIfContentConsumed();

            using Stream compressionStream = _createCompressionStream(stream);
            _originalContent.CopyTo(compressionStream, context, cancellationToken);
        }

        public async Task SerializeToStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            ThrowIfContentConsumed();

            Stream compressionStream = _createCompressionStream(stream);
            await using (compressionStream.ConfigureAwait(false))
            {
                await _originalContent.CopyToAsync(compressionStream, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose() => _originalContent.Dispose();

        private void ThrowIfContentConsumed()
        {
            if (_contentConsumed)
            {
                throw new InvalidOperationException(SR.net_http_content_stream_already_read);
            }

            _contentConsumed = true;
        }
    }
}
