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

        // RFC 9659 requires zstd decoders for the "zstd" content coding to support a window size of at
        // least 8 MB (2^23) and recommends that encoders not produce frames requiring a larger window.
        // Some compression levels (notably CompressionLevel.SmallestSize) would otherwise select a larger
        // window, producing payloads that a conformant server would reject. See RFC 9659, Section 3.
        private const int RfcMaxWindowLog2 = 23;
        private static readonly ZstandardCompressionOptions s_smallestSizeRfcOptions = new ZstandardCompressionOptions
        {
            Quality = ZstandardCompressionOptions.MaxQuality,
            WindowLog2 = RfcMaxWindowLog2
        };

        private readonly HttpContent _content;
        private readonly ZstandardCompressionOptions? _compressionOptions;
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardCompressedContent"/> class that compresses the
        /// provided content using the Zstandard content coding at the specified compression level.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency.</param>
        /// <remarks>
        /// RFC 9659 requires that the "zstd" content coding be decodable with a window size of 8 MB (2^23) and
        /// recommends that encoders not produce frames requiring a larger window. When setting <paramref name="compressionLevel"/>
        /// to <see cref="CompressionLevel.SmallestSize"/>, this class applies RFC-compliant options to limit the window size
        /// so that the produced content is accepted by servers that enforce this limit.
        /// </remarks>
        public ZstandardCompressedContent(HttpContent content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            ArgumentNullException.ThrowIfNull(content);
            CompressedContentCore.ValidateCompressionLevel(compressionLevel, nameof(compressionLevel));

            if (compressionLevel == CompressionLevel.SmallestSize)
            {
                // use RFC-compliant options for SmallestSize to avoid producing frames that a conformant server would reject
                _compressionOptions = s_smallestSizeRfcOptions;
            }
            else
            {
                _compressionLevel = compressionLevel;
            }

            _content = content;
            CompressedContentCore.InitializeHeaders(this, content, Encoding);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardCompressedContent"/> class that compresses the
        /// provided content using the Zstandard content coding and the specified compression options.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="compressionOptions">The options used to fine-tune the compression.</param>
        /// <remarks>
        /// RFC 9659 requires that the "zstd" content coding be decodable with a window size of 8 MB (2^23) and
        /// recommends that encoders not produce frames requiring a larger window. When supplying custom options,
        /// consider limiting <see cref="ZstandardCompressionOptions.WindowLog2"/> to 23 or less so that the
        /// produced content is accepted by servers that enforce this limit.
        /// </remarks>
        public ZstandardCompressedContent(HttpContent content, ZstandardCompressionOptions compressionOptions)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(compressionOptions);

            _compressionOptions = compressionOptions;
            _content = content;
            CompressedContentCore.InitializeHeaders(this, content, Encoding);
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            CompressedContentCore.SerializeToStream(_content, CreateCompressionStream(stream), context, cancellationToken);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            CompressedContentCore.SerializeToStreamAsync(_content, CreateCompressionStream(stream), context, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            CompressedContentCore.SerializeToStreamAsync(_content, CreateCompressionStream(stream), context, cancellationToken);

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
                _content.Dispose();
            }

            base.Dispose(disposing);
        }

        private ZstandardStream CreateCompressionStream(Stream outputStream) =>
            _compressionOptions is null
                ? new ZstandardStream(outputStream, _compressionLevel, leaveOpen: true)
                : new ZstandardStream(outputStream, _compressionOptions, leaveOpen: true);
    }
}
