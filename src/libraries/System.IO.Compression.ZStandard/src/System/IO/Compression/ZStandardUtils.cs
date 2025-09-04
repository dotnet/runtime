// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static class ZStandardUtils
    {
        // ZStandard compression level constants
        // These will be populated from P/Invoke calls to the native library
        internal const int DefaultCompressionLevel = 3;  // ZSTD_CLEVEL_DEFAULT
        internal const int MinCompressionLevel = -131072; // ZSTD_minCLevel() - approximation, will be updated from native
        internal const int MaxCompressionLevel = 22;      // ZSTD_MAX_CLEVEL

        // Buffer sizes for ZStandard operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli
    }
}
