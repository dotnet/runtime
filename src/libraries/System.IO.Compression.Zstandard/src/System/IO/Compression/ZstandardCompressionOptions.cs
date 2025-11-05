// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Provides compression options to be used with Zstandard compression.</summary>
    public sealed class ZstandardCompressionOptions
    {
        /// <summary>The default compression quality level.</summary>
        public static int DefaultQuality => ZstandardUtils.Quality_Default;

        /// <summary>The minimum compression quality level.</summary>
        public static int MinQuality => ZstandardUtils.Quality_Min;

        /// <summary>The maximum compression quality level.</summary>
        public static int MaxQuality => ZstandardUtils.Quality_Max;

        /// <summary>The default window size to use for Zstandard compression.</summary>
        public static int DefaultWindow => ZstandardUtils.WindowBits_Default;

        /// <summary>The minimum window size to use for Zstandard compression.</summary>
        public static int MinWindow => ZstandardUtils.WindowBits_Min;

        /// <summary>The maximum window size to use for Zstandard compression.</summary>
        public static int MaxWindow => ZstandardUtils.WindowBits_Max;

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class.</summary>
        public ZstandardCompressionOptions()
        {
        }

        /// <summary>Gets the compression quality to use for Zstandard compression.</summary>
        /// <value>The compression quality. The valid range is from <see cref="MinQuality"/> to <see cref="MaxQuality"/>.</value>
        /// <remarks>
        /// The compression quality determines the compression ratio and speed.
        /// Negative values extend the range of speed vs. ratio preferences, where lower levels are faster but provide less compression.
        /// Values 1-22 are normal compression levels, with <see cref="DefaultQuality"/> being the default.
        /// Values 20-22 require more memory and should be used with caution.
        /// </remarks>
        public int Quality { get; set; }

        /// <summary>Gets the window size to use for Zstandard compression.</summary>
        /// <value>The window size for compression.</value>
        public int Window { get; set; }

        /// <summary>Gets the dictionary to use for compression. If set, the quality specified during dictionary creation will take precedence over the <see cref="Quality"/> property.</summary>
        /// <value>The compression dictionary, or null if no dictionary is used.</value>
        public ZstandardDictionary? Dictionary { get; set; }

        /// <summary>Gets or sets a hint for the size of the block sizes that the encoder will output. Smaller size leads to more frequent outputs and lower latency when streaming.</summary>
        /// <value>The target block size in bytes. Valid range is from 1340 to 131072 (2^17). A value of 0 indicates no hint (implementation-defined behavior).</value>
        public int TargetBlockSize { get; set; }

        /// <summary>If <lang keyword="true"/>, will append a 32-bit checksum at the end of the compressed data.</summary>
        public bool AppendChecksum { get; set; }

        /// <summary>Gets or sets a value indicating whether long distance matching is enabled. This may improve compression ratios for large files at the cost of larger memory usage.</summary>
        public bool EnableLongDistanceMatching { get; set; }
    }
}
