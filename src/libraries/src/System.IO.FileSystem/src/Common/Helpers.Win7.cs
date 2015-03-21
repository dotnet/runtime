// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Helpers
{
    internal static SafeFileHandle CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        System.IO.FileShare dwShareMode,
        [In] ref Interop.mincore.SECURITY_ATTRIBUTES securityAttrs,
        System.IO.FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile)
    {
        return Interop.mincore.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    }

    internal static int CopyFile(string src, string dst, bool failIfExists)
    {
        int copyFlags = failIfExists ? Interop.mincore.FileOperations.COPY_FILE_FAIL_IF_EXISTS : 0;
        int cancel = 0;
        if (!Interop.mincore.CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, copyFlags))
        {
            return Marshal.GetLastWin32Error();
        }

        return Interop.mincore.Errors.ERROR_SUCCESS;
    }
}
