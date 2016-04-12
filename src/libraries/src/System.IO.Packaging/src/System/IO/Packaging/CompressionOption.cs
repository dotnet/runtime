// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Packaging
{
    /// <summary>
    /// This class is used to control Compression for package parts.  
    /// </summary>
    public enum CompressionOption : int
    {
        /// <summary>
        /// Compression is turned off in this mode.
        /// </summary>
        NotCompressed = -1,

        /// <summary>
        /// Compression is optimized for a reasonable compromise between size and performance. 
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Compression is optimized for size. 
        /// </summary>
        Maximum = 1,

        /// <summary>
        /// Compression is optimized for performance. 
        /// </summary>
        Fast = 2,

        /// <summary>
        /// Compression is optimized for super performance. 
        /// </summary>
        SuperFast = 3,
    }
}
