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

    internal static class CompressionFormatHelper
    {
        /// <summary>
        /// Resolves a windowLog value (8-15) to the windowBits parameter expected by zlib,
        /// based on the compression format. A windowLog of -1 is resolved to the default (15).
        /// </summary>
        /// <remarks>
        /// zlib-ng rejects windowBits 8 for raw deflate and gzip; classic zlib silently upgrades to 9.
        /// This method clamps to 9 for Deflate and GZip formats to match classic zlib behavior.
        /// </remarks>
        internal static int ResolveWindowBits(int windowLog, CompressionFormat format)
        {
            if (windowLog == -1)
            {
                windowLog = ZLibNative.DefaultWindowLog;
            }

            if (format != CompressionFormat.ZLib)
            {
                windowLog = Math.Max(windowLog, 9);
            }

            return format switch
            {
                CompressionFormat.Deflate => -windowLog,
                CompressionFormat.ZLib => windowLog,
                CompressionFormat.GZip => windowLog + 16,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }
    }
}
