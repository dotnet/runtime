// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// DisableRingBufferReallocation - Disables the lazy growth of the ring buffer, so that the full window is allocated upfront.
    /// LargeWindow - Flag that determines if "Large Window Brotli" is used, allowing window sizes up to 2^30 instead of the 2^24 maximum defined by RFC 7932.
    /// </summary>
    internal enum BrotliDecoderParameter
    {
        DisableRingBufferReallocation,
        LargeWindow
    }
}
