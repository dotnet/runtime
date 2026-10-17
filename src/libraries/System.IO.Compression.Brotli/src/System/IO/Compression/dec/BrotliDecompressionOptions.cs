// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Provides decompression options to be used with <see cref="BrotliStream"/>.
    /// </summary>
    public sealed class BrotliDecompressionOptions
    {
        private int _maxWindowLog2 = BrotliUtils.WindowBits_Max;

        /// <summary>
        /// Gets or sets the maximum base-2 logarithm of the window size accepted when decompressing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is less than 10 or greater than 30.</exception>
        /// <remarks>
        /// A Brotli stream declares the window size it requires in its header; this property does not select a window size, it
        /// rejects streams that ask for more than the specified value. Range is from 10 to 30. The default value is 24, the maximum
        /// defined by RFC 7932. Values greater than 24 additionally enable the "Large Window Brotli" extension, which is not part of
        /// RFC 7932. Because the decoder only allocates the memory a stream actually uses, raising this value does not by itself
        /// increase memory consumption; it raises the upper bound of what a stream is permitted to request.
        /// </remarks>
        public int MaxWindowLog2
        {
            get => _maxWindowLog2;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, BrotliUtils.WindowBits_Min, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BrotliUtils.WindowBits_MaxLarge, nameof(value));

                _maxWindowLog2 = value;
            }
        }
    }
}
