// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static partial class BrotliUtils
    {
        public const int WindowBits_Min = 10;
        public const int WindowBits_Default = 22;
        public const int WindowBits_Max = 24;
        public const int Quality_Min = 0;
        public const int Quality_Default = 4;
        public const int Quality_Max = 11;

        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                CompressionLevel.NoCompression => Quality_Min,
                // We use quality 2 for Fastest instead of 0 or 1 because qualities 0 and 1 produce very poor
                // compression for incremental writes (e.g., line-by-line), often resulting in output larger
                // than the uncompressed input. Quality 2 provides much better compression for such scenarios
                // while still maintaining fast compression speed.
                CompressionLevel.Fastest => 2,
                CompressionLevel.Optimal => Quality_Default,
                CompressionLevel.SmallestSize => Quality_Max,
                _ => throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(compressionLevel))
            };
    }
}
