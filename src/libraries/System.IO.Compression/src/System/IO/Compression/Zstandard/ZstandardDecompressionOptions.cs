// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Provides decompression options to be used with Zstandard decompression.</summary>
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("wasi")]
    public sealed class ZstandardDecompressionOptions
    {

        /// <summary>Gets or sets the maximum allowed base-2 logarithm of the window size when decompressing payloads.</summary>
        /// <value>The maximum allowed base-2 logarithm of the window size for decompression.</value>
        /// <remarks>
        /// The valid range is from <see cref="ZstandardCompressionOptions.MinWindowLog2"/> to <see cref="ZstandardCompressionOptions.MaxWindowLog2"/>.
        /// Value 0 indicates the implementation-defined default window size.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not 0 and is not between <see cref="ZstandardCompressionOptions.MinWindowLog2"/> and <see cref="ZstandardCompressionOptions.MaxWindowLog2"/>.</exception>
        public int MaxWindowLog2
        {
            get;
            set
            {
                if (value != 0)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, ZstandardUtils.WindowLog_Min, nameof(value));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, ZstandardUtils.WindowLog_Max, nameof(value));
                }

                field = value;
            }
        }

        /// <summary>Gets or sets the dictionary to use for decompression.</summary>
        /// <value>The decompression dictionary, or <see langword="null"/> if no dictionary is used.</value>
        public ZstandardDictionary? Dictionary { get; set; }
    }
}
