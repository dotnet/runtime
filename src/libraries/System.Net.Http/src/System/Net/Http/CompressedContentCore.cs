// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
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

        public CompressedContentCore(HttpContent originalContent, Func<Stream, Stream> createCompressionStream)
        {
            _originalContent = originalContent;
            _createCompressionStream = createCompressionStream;
        }

        public static void InitializeHeaders(HttpContent target, HttpContent source, string encoding)
        {
            // Copy headers from the original content.
            target.Headers.AddHeaders(source.Headers);

            // Append our encoding to the Content-Encoding header (supports stacking per HTTP spec).
            // The common case is that the inner content has no Content-Encoding, so take the
            // allocation-free fast path and only materialize the collection when we need to stack.
            if (target.Headers.Contains(KnownHeaders.ContentEncoding.Descriptor))
            {
                target.Headers.ContentEncoding.Add(encoding);
            }
            else
            {
                target.Headers.TryAddWithoutValidation(KnownHeaders.ContentEncoding.Descriptor, encoding);
            }

            // Remove Content-Length since we don't know the compressed size upfront.
            target.Headers.ContentLength = null;
        }

        public void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            using Stream compressionStream = _createCompressionStream(stream);
            _originalContent.CopyTo(compressionStream, context, cancellationToken);
        }

        public async Task SerializeToStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            Stream compressionStream = _createCompressionStream(stream);
            await using (compressionStream.ConfigureAwait(false))
            {
                await _originalContent.CopyToAsync(compressionStream, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose() => _originalContent.Dispose();
    }
}
