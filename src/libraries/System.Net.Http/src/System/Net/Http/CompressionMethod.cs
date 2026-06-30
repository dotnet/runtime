// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

/// <summary>
/// Specifies the compression method used by <see cref="CompressedContent"/> to compress request content.
/// </summary>
public enum CompressionMethod
{
    /// <summary>
    /// GZip compression, corresponding to the <c>gzip</c> content coding.
    /// </summary>
    GZip,

    /// <summary>
    /// Deflate (zlib) compression, corresponding to the <c>deflate</c> content coding.
    /// </summary>
    Deflate,

    /// <summary>
    /// Brotli compression, corresponding to the <c>br</c> content coding.
    /// </summary>
    Brotli,

    /// <summary>
    /// Zstandard compression, corresponding to the <c>zstd</c> content coding.
    /// </summary>
    Zstandard,
}
