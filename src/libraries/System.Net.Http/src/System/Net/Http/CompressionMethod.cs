// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

/// <summary>
/// Specifies the compression method used by <see cref="CompressedContent"/> to compress request content.
/// </summary>
public enum CompressionMethod
{
    GZip = 0x1,
    Deflate = 0x2,
    Brotli = 0x4,
    Zstandard = 0x8
}
