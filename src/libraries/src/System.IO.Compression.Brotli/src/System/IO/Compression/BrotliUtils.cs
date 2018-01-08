// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    internal static partial class BrotliUtils
    {
        public const int WindowBits_Min = 10;
        public const int WindowBits_Max = 24;
        public const int Quality_Min = 0;
        public const int Quality_Max = 11;
        public const int MaxInputSize = 2147483132; // 2^32 - 1 - 515 (max compressed extra bytes)

        internal static int GetQualityFromCompressionLevel(CompressionLevel level)
        {
            if (level == CompressionLevel.Optimal) // BROTLI_DEFAULT_QUALITY
                return 11;
            if (level == CompressionLevel.NoCompression) // BROTLI_MIN_QUALITY
                return 0;
            if (level == CompressionLevel.Fastest)
                return 1;
            return (int)level;
        }
    }
}
