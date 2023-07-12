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
        private static unsafe partial bool GetVolumeInformationByHandleW(
            SafeFileHandle hFile,
            char* volumeName,
            uint volumeNameBufLen,
            uint* volSerialNumber,
            uint* maxFileNameLen,
            uint* fileSystemFlags,
            char* fileSystemName,
            uint fileSystemNameBufLen);

        public static unsafe int GetVolumeInformationByHandle(
            SafeFileHandle hFile,
            uint* volSerialNumber,
            uint* maxFileNameLen,
            out uint fileSystemFlags,
            char* fileSystemName,
            uint fileSystemNameBufLen)
        {
            // Try to get the volume information.
            fixed (uint* pFileSystemFlags = &fileSystemFlags)
            {
                if (GetVolumeInformationByHandleW(hFile, null, 0, volSerialNumber, maxFileNameLen, pFileSystemFlags, fileSystemName, fileSystemNameBufLen))
                {
                    return 0;
                }
            }

            // Return the error.
            int error = Marshal.GetLastWin32Error();
            Debug.Assert(error != Errors.ERROR_INSUFFICIENT_BUFFER);
            return error;
        }
    }
}
