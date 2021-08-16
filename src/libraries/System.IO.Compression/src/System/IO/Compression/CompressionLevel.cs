// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies values that indicate whether a compression operation emphasizes speed or compression size.
    /// </summary>

    // This is an abstract concept and NOT the ZLib compression level.
    // There may or may not be any correspondence with the a possible implementation-specific level-parameter of the deflater.
    public enum CompressionLevel
    {
        /// <summary>
        /// The compression operation should balance compression speed and output size.
        /// </summary>
        Optimal = 0,

        /// <summary>
        /// The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.
        /// </summary>
        Fastest = 1,

        /// <summary>
        /// No compression should be performed on the file.
        /// </summary>
        NoCompression = 2,

        /// <summary>
        /// The compression operation should create output as small as possible, even if the operation takes a longer time to complete.
        /// </summary>
        SmallestSize = 3,
    }
}
