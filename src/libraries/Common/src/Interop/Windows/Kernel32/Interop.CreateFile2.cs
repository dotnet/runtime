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
        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh449422.aspx
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFile2", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern IntPtr CreateFile2(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            [In] ref CREATEFILE2_EXTENDED_PARAMETERS parameters);

        private static unsafe IntPtr CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            SECURITY_ATTRIBUTES* securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            CREATEFILE2_EXTENDED_PARAMETERS parameters;
            parameters.dwSize = (uint)Marshal.SizeOf<CREATEFILE2_EXTENDED_PARAMETERS>();

            parameters.dwFileAttributes = (uint)dwFlagsAndAttributes & 0x0000FFFF;
            parameters.dwSecurityQosFlags = (uint)dwFlagsAndAttributes & 0x000F0000;
            parameters.dwFileFlags = (uint)dwFlagsAndAttributes & 0xFFF00000;

            parameters.hTemplateFile = hTemplateFile;
            parameters.lpSecurityAttributes = securityAttrs;

            return CreateFile2(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref parameters);
        }

        internal static unsafe SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            fixed (SECURITY_ATTRIBUTES* lpSecurityAttributes = &securityAttrs)
            {
                IntPtr handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
                try
                {
                    return new SafeFileHandle(handle, ownsHandle: true);
                }
                catch
                {
                    CloseHandle(handle);
                    throw;
                }
            }
        }

        internal static unsafe SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes)
        {
            IntPtr handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, null, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
            try
            {
                return new SafeFileHandle(handle, ownsHandle: true);
            }
            catch
            {
                CloseHandle(handle);
                throw;
            }
        }

        internal unsafe static IntPtr CreateFile_IntPtr(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes)
        {
            lpFileName = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
            return CreateFile(lpFileName, dwDesiredAccess, dwShareMode, null, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
        }
    }
}
