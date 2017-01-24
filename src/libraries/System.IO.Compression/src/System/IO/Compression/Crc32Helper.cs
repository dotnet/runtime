// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// File implements Slicing-by-8 CRC Generation, as described in
// "Novel Table Lookup-Based Algorithms for High-Performance CRC Generation"
// IEEE TRANSACTIONS ON COMPUTERS, VOL. 57, NO. 11, NOVEMBER 2008

/*
 * Copyright (c) 2004-2006 Intel Corporation - All Rights Reserved
 *
 *
 * This software program is licensed subject to the BSD License,
 * available at http://www.opensource.org/licenses/bsd-license.html.
 */

using System.Diagnostics;

namespace System.IO.Compression
{
    /// <summary>
    /// This class contains a managed Crc32 function as well as an indirection to the Interop.Zlib.Crc32 call.
    /// Since Desktop compression uses this file alongside the Open ZipArchive, we cannot remove it
    /// without breaking the Desktop build.
    ///
    /// Note that in CoreFX the ZlibCrc32 function is always called.
    /// </summary>
    internal static class Crc32Helper
    {
        // Calculate CRC based on the old CRC and the new bytes
        // See RFC1952 for details.
        public static uint UpdateCrc32(uint crc32, byte[] buffer, int offset, int length)
        {
            Debug.Assert((buffer != null) && (offset >= 0) && (length >= 0)
                       && (offset <= buffer.Length - length), "check the caller");
            return Interop.zlib.crc32(crc32, buffer, offset, length);
        }
    }
}
