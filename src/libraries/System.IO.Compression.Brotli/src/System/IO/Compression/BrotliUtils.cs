// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    internal static partial class BrotliUtils
    {
        public const int WindowBits_Min = 10;
        public const int WindowBits_Default = 22;
        public const int WindowBits_Max = 24;
        public const int Quality_Min = 0;
        public const int Quality_Default = 11;
        public const int Quality_Max = 11;
        public const int MaxInputSize = int.MaxValue - 515; // 515 is the max compressed extra bytes

        internal static int GetQualityFromCompressionLevel(CompressionLevel level)
        {
            switch (level)
            {
                case CompressionLevel.Optimal:
                    return Quality_Default;
                case CompressionLevel.NoCompression:
                    return Quality_Min;
                case CompressionLevel.Fastest:
                    return 1;
                default:
                    return (int)level;
            }
        }
    }
}
