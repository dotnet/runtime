// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// FileSystem types used for special handling.
        /// </summary>
        /// <remarks>
        /// As values, we use Linux magic numbers. These values are Linux specific. Mapping for other platforms happens in PAL.
        /// </remarks>
        internal enum UnixFileSystemTypes : uint
        {
            nfs = 0x6969,
            cifs = 0xFF534D42,
            smb = 0x517B,
            smb2 = 0xFE534D42,
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFileSystemType")]
        private static partial uint GetFileSystemType(SafeFileHandle fd);

        internal static bool TryGetFileSystemType(SafeFileHandle fd, out UnixFileSystemTypes fileSystemType)
        {
            uint fstatfsResult = GetFileSystemType(fd);
            fileSystemType = (UnixFileSystemTypes)fstatfsResult;
            Debug.Assert(Enum.IsDefined(fileSystemType) || fstatfsResult == 0, $"GetFileSystemType returned {fstatfsResult}");
            return fstatfsResult != 0;
        }
    }
}
