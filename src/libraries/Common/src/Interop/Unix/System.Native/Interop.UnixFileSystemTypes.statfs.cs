// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFileSystemType")]
        private static extern long GetFileSystemType(SafeFileHandle fd);

        internal static bool TryGetFileSystemType(SafeFileHandle fd, out UnixFileSystemTypes fileSystemType)
        {
            long fstatfsResult = GetFileSystemType(fd);
            fileSystemType = (UnixFileSystemTypes)fstatfsResult;
            return fstatfsResult != -1;
        }
    }
}
