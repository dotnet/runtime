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

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class.</summary>
        public ZstandardCompressionOptions()
        {
            Quality = DefaultQuality;
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class with the specified compression level.</summary>
        /// <param name="level">One of the enumeration values that indicates whether to emphasize speed or compression efficiency.</param>
        public ZstandardCompressionOptions(CompressionLevel level) : this(ZstandardUtils.GetQualityFromCompressionLevel(level))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class with the specified dictionary.</summary>
        /// <param name="dictionary">The compression dictionary to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ZstandardCompressionOptions(ZstandardDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (dictionary.CompressionDictionary is null)
            {
                throw new ArgumentException(SR.ZstandardEncoder_InvalidDictionary);
            }

            Dictionary = dictionary;
            Quality = dictionary.CompressionDictionary.Quality;
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardCompressionOptions"/> class with the specified quality.</summary>
        /// <param name="quality">The compression quality level.</param>
        /// <exception cref="ArgumentOutOfRangeException">The quality is less than <see cref="MinQuality"/> or greater than <see cref="MaxQuality"/>.</exception>
        public ZstandardCompressionOptions(int quality)
        {
            if (quality < MinQuality || quality > MaxQuality)
                throw new ArgumentOutOfRangeException(nameof(quality), $"Quality must be between {MinQuality} and {MaxQuality}.");
            Quality = quality;
        }

        /// <summary>Gets the compression quality to use for Zstandard compression.</summary>
        /// <value>The compression quality. The valid range is from <see cref="MinQuality"/> to <see cref="MaxQuality"/>.</value>
        /// <remarks>
        /// The compression quality determines the compression ratio and speed.
        /// Negative values extend the range of speed vs. ratio preferences, where lower levels are faster but provide less compression.
        /// Values 1-22 are normal compression levels, with <see cref="DefaultQuality"/> being the default.
        /// Values 20-22 require more memory and should be used with caution.
        /// </remarks>
        public int Quality { get; }

        /// <summary>Gets the dictionary to use for compression.</summary>
        /// <value>The compression dictionary, or null if no dictionary is used.</value>
        public ZstandardDictionary? Dictionary { get; }
    }
}
