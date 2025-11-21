// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Provides compression options to be used with <see cref="BrotliStream"/>.
    /// </summary>
    public sealed class BrotliCompressionOptions
    {
        private int _quality = BrotliUtils.Quality_Default;
        private int _windowSize = BrotliUtils.WindowBits_Default;

        /// <summary>
        /// Gets or sets the compression quality for a Brotli compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is less than 0 or greater than 11.</exception>
        /// <remarks>
        /// The higher the quality, the slower the compression. Range is from 0 to 11. The default value is 4.
        /// </remarks>
        public int Quality
        {
            get => _quality;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 11, nameof(value));

                _quality = value;
            }
        }

        /// <summary>
        /// Gets or sets the window size for a Brotli compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is less than 10 or greater than 24.</exception>
        /// <remarks>
        /// The window size is the sliding window size in bits used by the LZ77 algorithm. Larger window sizes can improve compression ratio but use more memory. Range is from 10 to 24. The default value is 22.
        /// </remarks>
        public int WindowSize
        {
            get => _windowSize;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, BrotliUtils.WindowBits_Min, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BrotliUtils.WindowBits_Max, nameof(value));

                _windowSize = value;
            }
        }
    }
}
