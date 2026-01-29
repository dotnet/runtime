// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Provides compression options to be used with Zstandard compression.</summary>
    public sealed class ZstandardCompressionOptions
    {
        /// <summary>Gets the default compression quality level.</summary>
        public static int DefaultQuality => ZstandardUtils.Quality_Default;

        /// <summary>Gets the minimum compression quality level.</summary>
        public static int MinQuality => ZstandardUtils.Quality_Min;

        /// <summary>Gets the maximum compression quality level.</summary>
        public static int MaxQuality => ZstandardUtils.Quality_Max;

        /// <summary>Gets the default window size to use for Zstandard compression.</summary>
        public static int DefaultWindowLog => ZstandardUtils.WindowLog_Default;

        /// <summary>Gets the minimum window size to use for Zstandard compression.</summary>
        public static int MinWindowLog => ZstandardUtils.WindowLog_Min;

        /// <summary>Gets the maximum window size to use for Zstandard compression.</summary>
        public static int MaxWindowLog => ZstandardUtils.WindowLog_Max;

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class.</summary>
        public ZstandardCompressionOptions()
        {
        }

        /// <summary>Gets or sets the compression quality to use for Zstandard compression.</summary>
        /// <value>The compression quality. The valid range is from <see cref="MinQuality"/> to <see cref="MaxQuality"/>.</value>
        /// <remarks>
        /// The compression quality determines the compression ratio and speed.
        /// Negative values extend the range of speed versus ratio preferences, where lower levels are faster but provide less compression.
        /// Value 0 indicates the implementation-defined default quality.
        /// Values 1-22 are normal compression levels, with <see cref="DefaultQuality"/> being the default.
        /// Values 20-22 require more memory and should be used with caution.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not 0 and is not between <see cref="MinQuality"/> and <see cref="MaxQuality"/>.</exception>
        public int Quality
        {
            get;
            set
            {
                if (value != 0)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, ZstandardUtils.Quality_Max, nameof(value));
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, ZstandardUtils.Quality_Min, nameof(value));
                }

                field = value;
            }
        }

        /// <summary>Gets or sets the window size to use for Zstandard compression.</summary>
        /// <value>The window size for compression, expressed as base 2 logarithm.</value>
        /// <remarks>
        /// The window size determines how much data the compressor can reference for finding matches.
        /// Larger window sizes can improve compression ratios for large files but require more memory.
        /// The valid range is from <see cref="MinWindowLog"/> to <see cref="MaxWindowLog"/>.
        /// Value 0 indicates the implementation-defined default window size.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not 0 and is not between <see cref="MinWindowLog"/> and <see cref="MaxWindowLog"/>.</exception>
        public int WindowLog
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

        /// <summary>Gets or sets the dictionary to use for compression. If set, the quality specified during dictionary creation takes precedence over the <see cref="Quality"/> property.</summary>
        /// <value>The compression dictionary, or null if no dictionary is used.</value>
        public ZstandardDictionary? Dictionary { get; set; }

        /// <summary>Gets or sets a hint for the size of the block sizes that the encoder will output. Smaller size leads to more frequent outputs and lower latency when streaming.</summary>
        /// <value>The target block size in bytes. Valid range is from 1340 to 131072 (2^17). A value of 0 indicates no hint (implementation-defined behavior).</value>
        public int TargetBlockSize
        {
            get;
            set
            {
                if (value != 0)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, ZstandardUtils.TargetBlockSize_Min, nameof(value));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, ZstandardUtils.TargetBlockSize_Max, nameof(value));
                }

                field = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether a 32-bit checksum will be appended at the end of the compressed data.</summary>
        public bool AppendChecksum { get; set; }

        /// <summary>Gets or sets a value indicating whether long-distance matching is enabled.</summary>
        /// <value><see langword="true"/> if long-distance matching is enabled; otherwise, <see langword="false"/>.</value>
        /// <remarks>Setting this property to <see langword="true" /> might improve compression ratios for large files at the cost of higher memory usage.</remarks>
        public bool EnableLongDistanceMatching { get; set; }
    }
}
