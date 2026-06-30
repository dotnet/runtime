// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Provides HTTP content that compresses the inner content using a specified encoding.
    /// </summary>
    public sealed class CompressedContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private readonly CompressionMethod _method;
        private readonly CompressionLevel _compressionLevel;
        private bool _contentConsumed;

        /// <summary>
        /// Initializes a new instance of <see cref="CompressedContent"/> that compresses the provided content
        /// using the specified compression method with <see cref="CompressionLevel.Optimal"/>.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="method">The compression method to use.</param>
        public CompressedContent(HttpContent content, CompressionMethod method)
            : this(content, method, CompressionLevel.Optimal)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompressedContent"/> that compresses the provided content
        /// using the specified compression method and compression level.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="method">The compression method to use.</param>
        /// <param name="compressionLevel">The level of compression to use.</param>
        public CompressedContent(HttpContent content, CompressionMethod method, CompressionLevel compressionLevel)
        {
            ArgumentNullException.ThrowIfNull(content);

            // Resolve the wire encoding name, which also validates that the method is a defined value.
            string encoding = GetEncodingName(method);

            _originalContent = content;
            _method = method;
            _compressionLevel = compressionLevel;

            // Copy headers from the original content.
            foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Append our encoding to the Content-Encoding header (supports stacking per HTTP spec).
            Headers.ContentEncoding.Add(encoding);

            // Remove Content-Length since we don't know the compressed size upfront.
            Headers.ContentLength = null;
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ThrowIfContentConsumed();

            using Stream compressionStream = CreateCompressionStream(stream);
            _originalContent.CopyTo(compressionStream, context, cancellationToken);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ThrowIfContentConsumed();

            Stream compressionStream = CreateCompressionStream(stream);
            await using (compressionStream.ConfigureAwait(false))
            {
                await _originalContent.CopyToAsync(compressionStream, cancellationToken).ConfigureAwait(false);
            }
        }

        protected internal override bool TryComputeLength(out long length)
        {
            // Compressed size is not known without actually compressing.
            length = 0;
            return false;
        }

        internal override bool AllowDuplex => false;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalContent.Dispose();
            }

            base.Dispose(disposing);
        }

        private Stream CreateCompressionStream(Stream outputStream)
        {
            switch (_method)
            {
                case CompressionMethod.GZip:
                    return new GZipStream(outputStream, _compressionLevel, leaveOpen: true);

                case CompressionMethod.Deflate:
                    return new ZLibStream(outputStream, _compressionLevel, leaveOpen: true);

                case CompressionMethod.Brotli:
                    if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
                    {
                        throw new PlatformNotSupportedException();
                    }
                    return new BrotliStream(outputStream, _compressionLevel, leaveOpen: true);

                case CompressionMethod.Zstandard:
                    if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
                    {
                        throw new PlatformNotSupportedException();
                    }
                    return new ZstandardStream(outputStream, _compressionLevel, leaveOpen: true);

                default:
                    throw new ArgumentOutOfRangeException(nameof(_method));
            }
        }

        private void ThrowIfContentConsumed()
        {
            if (_contentConsumed)
            {
                throw new InvalidOperationException(SR.net_http_content_stream_already_read);
            }

            _contentConsumed = true;
        }

        private static string GetEncodingName(CompressionMethod method) => method switch
        {
            CompressionMethod.GZip => "gzip",
            CompressionMethod.Deflate => "deflate",
            CompressionMethod.Brotli => "br",
            CompressionMethod.Zstandard => "zstd",
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}
