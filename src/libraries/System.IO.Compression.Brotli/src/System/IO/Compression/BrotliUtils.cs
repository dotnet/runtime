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
        public const int MaxInputSize = int.MaxValue - 515; // 515 is the max compressed extra bytes

        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                CompressionLevel.NoCompression => Quality_Min,
                CompressionLevel.Fastest => 1,
                CompressionLevel.Optimal => Quality_Default,
                CompressionLevel.SmallestSize => Quality_Max,
                _ => throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(compressionLevel))
            };
    }
}
