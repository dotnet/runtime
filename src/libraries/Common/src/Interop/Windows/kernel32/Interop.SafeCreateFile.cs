// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        /// <summary>
        /// Does not allow access to non-file devices. This disallows DOS devices like "con:", "com1:",
        /// "lpt1:", etc.  Use this to avoid security problems, like allowing a web client asking a server
        /// for "http://server/com1.aspx" and then causing a worker process to hang.
        /// </summary>
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle SafeCreateFile(
            String lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref Interop.Kernel32.SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            SafeFileHandle handle = UnsafeCreateFile(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            if (!handle.IsInvalid)
            {
                int fileType = Interop.Kernel32.GetFileType(handle);
                if (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                {
                    handle.Dispose();
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }
            }

            return handle;
        }
    }
}
