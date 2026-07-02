// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Provides HTTP content that compresses the inner content using the Zstandard content coding.
    /// </summary>
    public sealed class ZstandardCompressedContent : HttpContent
    {
        private const string Encoding = "zstd";

        private readonly CompressedContentCore _core;
        private readonly ZstandardCompressionOptions? _compressionOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardCompressedContent"/> class that compresses the
        /// provided content using the Zstandard content coding with default compression settings.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        public ZstandardCompressedContent(HttpContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            _core = new CompressedContentCore(content, CreateCompressionStream);
            CompressedContentCore.InitializeHeaders(this, content, Encoding);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardCompressedContent"/> class that compresses the
        /// provided content using the Zstandard content coding and the specified compression options.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="compressionOptions">The options used to fine-tune the compression.</param>
        public ZstandardCompressedContent(HttpContent content, ZstandardCompressionOptions compressionOptions)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(compressionOptions);

            _compressionOptions = compressionOptions;
            _core = new CompressedContentCore(content, CreateCompressionStream);
            CompressedContentCore.InitializeHeaders(this, content, Encoding);
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            _core.SerializeToStream(stream, context, cancellationToken);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            _core.SerializeToStreamAsync(stream, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            _core.SerializeToStreamAsync(stream, cancellationToken);

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
                _core.Dispose();
            }

            base.Dispose(disposing);
        }

        private ZstandardStream CreateCompressionStream(Stream outputStream)
        {
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
            {
                throw new PlatformNotSupportedException();
            }

            return _compressionOptions is null
                ? new ZstandardStream(outputStream, CompressionLevel.Optimal, leaveOpen: true)
                : new ZstandardStream(outputStream, _compressionOptions, leaveOpen: true);
        }
    }
}
