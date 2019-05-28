// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use CreateFile.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern SafeFileHandle CreateFilePrivate(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES securityAttrs,
            System.IO.FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        internal static SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES securityAttrs,
            System.IO.FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            lpFileName = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName)!; // TODO-NULLABLE: Remove ! when nullable attributes are respected
            return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        }
    }
}
