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

        /// <summary>
        /// Gets or sets the compression quality for a Brotli compression stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set">Thrown when the value is less than 0 or greater than 11.</exception>
        /// <remarks>
        /// The higher the quality, the slower the compression. Range is from 0 to 11.
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
    }
}
