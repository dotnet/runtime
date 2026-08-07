// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies whether to compress or decompress the underlying stream.
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>Decompresses the underlying stream.</summary>
        Decompress = 0,

        /// <summary>Compresses the underlying stream.</summary>
        Compress = 1
    }
}
