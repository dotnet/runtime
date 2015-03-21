// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Helpers
{
    internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

    // Disallow access to all non-file devices from methods that take
    // a String.  This disallows DOS devices like "con:", "com1:", 
    // "lpt1:", etc.  Use this to avoid security problems, like allowing
    // a web client asking a server for "http://server/com1.aspx" and
    // then causing a worker process to hang.
    [System.Security.SecurityCritical]  // auto-generated
    internal static SafeFileHandle SafeCreateFile(
        String lpFileName,
        int dwDesiredAccess,
        System.IO.FileShare dwShareMode,
        ref Interop.mincore.SECURITY_ATTRIBUTES securityAttrs,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile)
    {
        SafeFileHandle handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

        if (!handle.IsInvalid)
        {
            int fileType = Interop.mincore.GetFileType(handle);
            if (fileType != Interop.mincore.FileTypes.FILE_TYPE_DISK)
            {
                handle.Dispose();
                throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
            }
        }

        return handle;
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal static SafeFileHandle UnsafeCreateFile(
        string lpFileName,
        int dwDesiredAccess,
        FileShare dwShareMode,
        ref Interop.mincore.SECURITY_ATTRIBUTES securityAttrs,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile)
    {
        return CreateFile(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    }
}
