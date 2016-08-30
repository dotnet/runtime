// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    /// <summary>
    /// Defines a tradeoff between fast vs. strong compression. The specific meaning depends of the Deflater implementation.
    /// </summary>

    // This is an abstract concept and NOT the ZLib compression level.
    // There may or may not be any correspondance with the a possible implementation-specific level-parameter of the deflater.
    public enum CompressionLevel
    {
        Optimal = 0,
        Fastest = 1,
        NoCompression = 2
    }  // internal enum CompressionLevel
}  // namespace System.IO.Compression
