// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Helpers
{
    internal static int CopyFile(String src, String dst, bool failIfExists)
    {
        uint copyFlags = failIfExists ? (uint)Interop.mincore.FileOperations.COPY_FILE_FAIL_IF_EXISTS : 0;
        Interop.mincore.COPYFILE2_EXTENDED_PARAMETERS parameters = new Interop.mincore.COPYFILE2_EXTENDED_PARAMETERS()
        {
            dwSize = (uint)Marshal.SizeOf<Interop.mincore.COPYFILE2_EXTENDED_PARAMETERS>(),
            dwCopyFlags = copyFlags
        };

        int hr = Interop.mincore.CopyFile2(src, dst, ref parameters);

        return Win32Marshal.TryMakeWin32ErrorCodeFromHR(hr);
    }

    private static unsafe SafeFileHandle CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        System.IO.FileShare dwShareMode,
        ref Interop.mincore.SECURITY_ATTRIBUTES securityAttrs,
        System.IO.FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile)
    {
        Interop.mincore.CREATEFILE2_EXTENDED_PARAMETERS parameters;
        parameters.dwSize = (uint)Marshal.SizeOf<Interop.mincore.CREATEFILE2_EXTENDED_PARAMETERS>();

        parameters.dwFileAttributes = (uint)dwFlagsAndAttributes & 0x0000FFFF;
        parameters.dwSecurityQosFlags = (uint)dwFlagsAndAttributes & 0x000F0000;
        parameters.dwFileFlags = (uint)dwFlagsAndAttributes & 0xFFF00000;

        parameters.hTemplateFile = hTemplateFile;
        fixed (Interop.mincore.SECURITY_ATTRIBUTES* lpSecurityAttributes = &securityAttrs)
        {
            parameters.lpSecurityAttributes = (IntPtr)lpSecurityAttributes;
            return Interop.mincore.CreateFile2(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref parameters);
        }
    }
    
    internal static int DeleteVolumeMountPoint(string mountPoint)
    {
        // DeleteVolumeMountPointW is not available to store apps.
        // The expectation is that no store app would even have permission
        // to call this from the app container
        throw new UnauthorizedAccessException();
    }
}

internal partial class Interop
{
    internal partial class mincore
    {
      internal static uint SetErrorMode(uint uMode)
      {
          // Prompting behavior no longer occurs in all platforms supported
          return 0;
      }
    }
}
