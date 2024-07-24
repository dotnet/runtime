// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Provides compression options to be used with <see cref="ZLibStream"/>, <see cref="DeflateStream"/> and <see cref="GZipStream"/>.
    /// </summary>
    public sealed class ZLibCompressionOptions
    {
        private int _compressionLevel;
        private ZLibCompressionStrategy _strategy;

        /// <summary>
        /// Gets or sets the compression level for a compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than -1 or greater than 9.</exception>"
        /// <remarks>
        /// Can accept any value between -1 and 9 (inclusive), 0 gives no compression,  1 gives best speed, 9 gives best compression.
        /// and -1 requests the default compression level which is currently equivalent to 6.
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
        /// <exception cref="ArgumentOutOfRangeException" >Thrown when the value is not a valid <see cref="ZLibCompressionStrategy"/> value.</exception>"
        public ZLibCompressionStrategy CompressionStrategy
        {
            get => _strategy;
            set
            {
                if (value < ZLibCompressionStrategy.Default || value > ZLibCompressionStrategy.Fixed)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _strategy = value;
            }
        }
    }

    /// <summary>
    /// Specifies the compression algorithm to use for compression stream.
    /// </summary>
    public enum ZLibCompressionStrategy
    {
        /// <summary>
        /// Used for normal data
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
