// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies the compression format for zlib-based encoders.
    /// </summary>
    internal enum CompressionFormat
    {
        /// <summary>Raw deflate format (no header/trailer).</summary>
        Deflate,
        /// <summary>ZLib format (zlib header/trailer).</summary>
        ZLib,
        /// <summary>GZip format (gzip header/trailer).</summary>
        GZip
    }
}
