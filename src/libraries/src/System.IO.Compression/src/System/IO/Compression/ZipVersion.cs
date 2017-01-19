// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    internal enum ZipVersionNeededValues : ushort
    {
        Default = 10,
        ExplicitDirectory = 20,
        Deflate = 20,
        Deflate64 = 21,
        Zip64 = 45
    }

    /// <summary>
    /// The upper byte of the "version made by" flag in the central directory header of a zip file represents the
    /// OS of the system on which the zip was created. Any zip created with an OS byte not equal to Windows (0)
    /// or Unix (3) will be treated as equal to the current OS.
    /// </summary>
    /// <remarks>
    /// The value of 0 more specifically corresponds to the FAT file system while NTFS is assigned a higher value. However
    /// for historical and compatibility reasons, Windows is always assigned a 0 value regardless of file system.
    /// </remarks>
    internal enum ZipVersionMadeByPlatform : byte
    {
        Windows = 0,
        Unix = 3
    }
}
