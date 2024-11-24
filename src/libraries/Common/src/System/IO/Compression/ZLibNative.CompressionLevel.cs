// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression;

internal static partial class ZLibNative
{
    /// <summary>
    /// <p>ZLib can accept any integer value between 0 and 9 (inclusive) as a valid compression level parameter:
    /// 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time).
    /// <code>CompressionLevel.DefaultCompression</code> = -1 requests a default compromise between speed and compression
    /// (currently equivalent to level 6).</p>
    ///
    /// <p><strong>How to choose a compression level:</strong></p>
    ///
    /// <p>The names <code>NoCompression</code>, <code>BestSpeed</code>, <code>DefaultCompression</code>, <code>BestCompression</code> are taken over from
    /// the corresponding ZLib definitions, which map to our public NoCompression, Fastest, Optimal, and SmallestSize respectively.</p>
    /// <p><em>Optimal Compression:</em></p>
    /// <p><code>ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.DefaultCompression;</code> <br />
    ///    <code>int windowBits = 15;  // or -15 if no headers required</code> <br />
    ///    <code>int memLevel = 8;</code> <br />
    ///    <code>ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;</code> </p>
    ///
    ///<p><em>Fastest compression:</em></p>
    ///<p><code>ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.BestSpeed;</code> <br />
    ///   <code>int windowBits = 15;  // or -15 if no headers required</code> <br />
    ///   <code>int memLevel = 8; </code> <br />
    ///   <code>ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;</code> </p>
    ///
    /// <p><em>No compression (even faster, useful for data that cannot be compressed such some image formats):</em></p>
    /// <p><code>ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.NoCompression;</code> <br />
    ///    <code>int windowBits = 15;  // or -15 if no headers required</code> <br />
    ///    <code>int memLevel = 7;</code> <br />
    ///    <code>ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;</code> </p>
    ///
    /// <p><em>Smallest Size Compression:</em></p>
    /// <p><code>ZLibNative.CompressionLevel compressionLevel = ZLibNative.CompressionLevel.BestCompression;</code> <br />
    ///    <code>int windowBits = 15;  // or -15 if no headers required</code> <br />
    ///    <code>int memLevel = 8;</code> <br />
    ///    <code>ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;</code> </p>
    /// </summary>
    public enum CompressionLevel : int
    {
        NoCompression = 0,
        BestSpeed = 1,
        DefaultCompression = -1,
        BestCompression = 9
    }
}
