// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Provides HTTP content that compresses the inner content using the Zstandard content coding.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardCompressedContent : HttpContent
    {
        private const string Encoding = "zstd";

        private readonly CompressedContentCore _core;
        private readonly ZstandardCompressionOptions? _compressionOptions;
        private readonly CompressionLevel _compressionLevel;

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
        /// provided content using the Zstandard content coding at the specified compression level.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency.</param>
        public ZstandardCompressedContent(HttpContent content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            ArgumentNullException.ThrowIfNull(content);

            _compressionLevel = compressionLevel;
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
            _core.SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            _core.SerializeToStreamAsync(stream, context, cancellationToken);

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
                ? new ZstandardStream(outputStream, _compressionLevel, leaveOpen: true)
                : new ZstandardStream(outputStream, _compressionOptions, leaveOpen: true);
        }
    }
}
