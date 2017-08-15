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
        /// WARNING: The private methods do not implicitly handle long paths. Use CreateFile.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        private static extern SafeFileHandle CreateFilePrivate(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            [In] ref SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        internal static SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            [In] ref SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            lpFileName = PathInternal.EnsureExtendedPrefixOverMaxPath(lpFileName);
            return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, ref securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        }

        internal static SafeFileHandle CreateFileDefaultSecurity(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes)
        {
            // This is technically equivalent to passing a null SECURITY_ATTRIBUTES
            SECURITY_ATTRIBUTES security = new SECURITY_ATTRIBUTES();
            return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, ref security, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
        }
    }
}
