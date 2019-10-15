// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    /// <summary>
    /// Defines a tradeoff between fast vs. strong compression. The specific meaning depends of the Deflater implementation.
    /// </summary>

    // This is an abstract concept and NOT the ZLib compression level.
    // There may or may not be any correspondence with the a possible implementation-specific level-parameter of the deflater.
    public enum CompressionLevel
    {
        Optimal = 0,
        Fastest = 1,
        NoCompression = 2
    }
}
