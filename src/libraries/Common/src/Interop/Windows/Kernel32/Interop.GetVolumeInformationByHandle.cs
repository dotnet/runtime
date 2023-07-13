// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumeInformationByHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetVolumeInformationByHandle(
            SafeFileHandle hFile,
            char* volumeName,
            uint volumeNameBufLen,
            uint* volSerialNumber,
            uint* maxFileNameLen,
            uint* fileSystemFlags,
            char* fileSystemName,
            uint fileSystemNameBufLen);
    }
}
