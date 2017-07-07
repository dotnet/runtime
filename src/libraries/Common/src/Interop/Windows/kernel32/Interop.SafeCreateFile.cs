// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// Does not allow access to non-file devices. This disallows DOS devices like "con:", "com1:",
        /// "lpt1:", etc.  Use this to avoid security problems, like allowing a web client asking a server
        /// for "http://server/com1.aspx" and then causing a worker process to hang.
        /// </summary>
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle SafeCreateFile(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            SafeFileHandle handle = UnsafeCreateFile(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            if (!handle.IsInvalid)
            {
                int fileType = GetFileType(handle);
                if (fileType != FileTypes.FILE_TYPE_DISK)
                {
                    handle.Dispose();
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }
            }

            return handle;
        }
    }
}
