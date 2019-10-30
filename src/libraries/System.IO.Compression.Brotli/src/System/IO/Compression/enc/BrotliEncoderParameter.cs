// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    /// <summary>
    /// Mode - BrotliEncoderMode enumerates all available values.
    /// Quality - The main compression speed-density lever. The higher the quality, the slower the compression. Range is from ::BROTLI_MIN_QUALITY to::BROTLI_MAX_QUALITY.
    /// LGWin - Recommended sliding LZ77 window size. Encoder may reduce this value, e.g. if input is much smaller than window size. Range is from BROTLI_MIN_WINDOW_BITS to BROTLI_MAX_WINDOW_BITS.
    /// LGBlock - Recommended input block size. Encoder may reduce this value, e.g. if input is much smaller than window size. Range is from BROTLI_MIN_INPUT_BLOCK_BITS to BROTLI_MAX_INPUT_BLOCK_BITS. Bigger input block size allows better compression, but consumes more memory.
    /// LCModeling-  Flag that affects usage of "literal context modeling" format feature. This flag is a "decoding-speed vs compression ratio" trade-off.
    /// SizeHint - Estimated total input size for all BrotliEncoderCompressStream calls. The default value is 0, which means that the total input size is unknown.
    /// </summary>
    internal enum BrotliEncoderParameter
    {
        Mode,
        Quality,
        LGWin,
        LGBlock,
        LCModeling,
        SizeHint
    }
}
