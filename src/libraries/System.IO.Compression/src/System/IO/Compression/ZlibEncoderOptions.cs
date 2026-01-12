// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Provides compression options for <see cref="ZlibEncoder"/>.
    /// </summary>
    public sealed class ZlibEncoderOptions
    {
        private int _compressionLevel = -1;
        private ZLibCompressionStrategy _compressionStrategy;
        private ZlibCompressionFormat _format = ZlibCompressionFormat.Deflate;

        /// <summary>
        /// Gets or sets the compression level for the encoder.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than -1 or greater than 9.</exception>
        /// <remarks>
        /// The compression level can be any value between -1 and 9 (inclusive).
        /// -1 requests the default compression level (currently equivalent to 6).
        /// 0 gives no compression.
        /// 1 gives best speed.
        /// 9 gives best compression.
        /// The default value is -1.
        /// </remarks>
        public int CompressionLevel
        {
            get => _compressionLevel;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 9);

                _compressionLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets the compression strategy for the encoder.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a valid <see cref="ZLibCompressionStrategy"/> value.</exception>
        public ZLibCompressionStrategy CompressionStrategy
        {
            get => _compressionStrategy;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan((int)value, (int)ZLibCompressionStrategy.Default, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan((int)value, (int)ZLibCompressionStrategy.Fixed, nameof(value));

                _compressionStrategy = value;
            }
        }

        /// <summary>
        /// Gets or sets the compression format for the encoder.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a valid <see cref="ZlibCompressionFormat"/> value.</exception>
        public ZlibCompressionFormat Format
        {
            get => _format;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan((int)value, (int)ZlibCompressionFormat.Deflate, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan((int)value, (int)ZlibCompressionFormat.GZip, nameof(value));

                _format = value;
            }
        }
    }
}
