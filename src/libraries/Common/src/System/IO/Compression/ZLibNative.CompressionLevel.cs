// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression;

internal static partial class ZLibNative
{
    /// <summary>
    /// ZLib can accept any integer value between 0 and 9 (inclusive) as a valid compression level parameter:
    /// 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time).
    /// <see cref="CompressionLevel.DefaultCompression" /> = -1 requests a default compromise between speed and compression
    /// (currently equivalent to level 6).
    /// </summary>
    /// <remarks>
    /// <para><strong>How to choose a compression level:</strong><br />
    /// The names <see cref="NoCompression" />, <see cref="BestSpeed" />, <see cref="DefaultCompression" />, <see cref="BestCompression" /> are taken over from
    /// the corresponding ZLib definitions, which map to our public NoCompression, Fastest, Optimal, and SmallestSize respectively.</para>
    /// <em>Optimal Compression:</em>
    /// <code>
    /// ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.DefaultCompression;
    /// int windowBits = 15;  // or -15 if no headers required
    /// int memLevel = 8;
    /// ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
    /// </code>
    ///
    /// <em>Fastest compression:</em>
    /// <code>
    /// ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.BestSpeed;
    /// int windowBits = 15;  // or -15 if no headers required
    /// int memLevel = 8;
    /// ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
    /// </code>
    ///
    /// <em>No compression (even faster, useful for data that cannot be compressed such some image formats):</em>
    /// <code>
    /// ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.NoCompression;
    /// int windowBits = 15;  // or -15 if no headers required
    /// int memLevel = 7;
    /// ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
    /// </code>
    ///
    /// <em>Smallest Size Compression:</em>
    /// <code>
    /// ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.BestCompression;
    /// int windowBits = 15;  // or -15 if no headers required
    /// int memLevel = 8;
    /// ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
    /// </code>
    /// </remarks>
    public enum CompressionLevel : int
    {
        NoCompression = 0,
        BestSpeed = 1,
        DefaultCompression = -1,
        BestCompression = 9
    }
}
