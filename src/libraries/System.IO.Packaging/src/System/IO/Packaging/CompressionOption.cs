// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  CompressionOption enumeration is used as an aggregate mechanism to give users controls 
//  over Compression features. 
//
//-----------------------------------------------------------------------------

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
        /// Compression is optimized for a resonable compromise between size and performance. 
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
