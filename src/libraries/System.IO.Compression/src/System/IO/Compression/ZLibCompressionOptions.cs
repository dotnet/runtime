// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Provides compression options to be used with <see cref="ZLibStream"/>, <see cref="DeflateStream"/> and <see cref="GZipStream"/>.
    /// </summary>
    public sealed class ZLibCompressionOptions
    {
        private const int MinWindowLog = 8;
        private const int MaxWindowLog = 15;

        private int _compressionLevel = -1;
        private ZLibCompressionStrategy _strategy;
        private int _windowLog = -1;

        /// <summary>
        /// Gets or sets the compression level for a compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than -1 or greater than 9.</exception>
        /// <remarks>
        /// Can accept any value between -1 and 9 (inclusive), 0 gives no compression,  1 gives best speed, 9 gives best compression.
        /// and -1 requests the default compression level which is currently equivalent to 6. The default value is -1.
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
        /// Gets or sets the compression algorithm for a compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" >The value is not a valid <see cref="ZLibCompressionStrategy"/> value.</exception>
        public ZLibCompressionStrategy CompressionStrategy
        {
            get => _strategy;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan((int)value, (int) ZLibCompressionStrategy.Default, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan((int)value, (int)ZLibCompressionStrategy.Fixed, nameof(value));

                _strategy = value;
            }
        }

        /// <summary>
        /// Gets or sets the base-2 logarithm of the window size for a compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than -1 or greater than 15, or between 0 and 7.</exception>
        /// <remarks>
        /// Can accept -1 or any value between 8 and 15 (inclusive). Larger values result in better compression at the expense of memory usage.
        /// -1 requests the default window log which is currently equivalent to 15 (32KB window). The default value is -1.
        /// </remarks>
        public int WindowLog
        {
            get => _windowLog;
            set
            {
                if (value != -1)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, MinWindowLog);
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxWindowLog);
                }

                _windowLog = value;
            }
        }
    }

    /// <summary>
    /// Defines  the compression algorithms that can be used for <see cref="DeflateStream"/>, <see cref="GZipStream"/> or <see cref="ZLibStream"/>.
    /// </summary>
    public enum ZLibCompressionStrategy
    {
        /// <summary>
        /// Used for normal data.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Used for data produced by a filter (or predictor). The effect of Filtered is to force more Huffman
        /// coding and less string matching, intermediate between Default and HuffmanOnly.
        /// </summary>
        Filtered = 1,
        /// <summary>
        /// Used to force Huffman encoding only (no string match).
        /// </summary>
        HuffmanOnly = 2,
        /// <summary>
        /// Used to limit match distances to one (run-length encoding), give better compression for PNG image data.
        /// </summary>
        RunLengthEncoding = 3,
        /// <summary>
        /// Prevents the use of dynamic Huffman codes, allowing for a simpler decoder for special applications.
        /// </summary>
        Fixed = 4
    }
}
