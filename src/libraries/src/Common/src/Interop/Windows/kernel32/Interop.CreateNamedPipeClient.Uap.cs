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
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFile2", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern SafePipeHandle CreateNamedPipeClientPrivate(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            System.IO.FileMode dwCreationDisposition,
            [In] ref CREATEFILE2_EXTENDED_PARAMETERS parameters);

        internal static unsafe SafePipeHandle CreateNamedPipeClient(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES secAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS parameters;
            parameters.dwSize = (uint)Marshal.SizeOf<Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS>();

            // The dwFlagsAndAttributes is carrying a combination of flags that are mapped to different fields of the extended
            // parameters. The possible range of values for dwFileAttributes, dwSecurityQosFlags, and dwFileFlags cannot be fully
            // covered coming from a single int but are enough for correction creation of the named pipe client. The SECURITY_VALID_SQOS_FLAGS
            // needs to be all available for proper impersonation.
            const uint SECURITY_VALID_SQOS_FLAGS = 0x001F0000;
            
            parameters.dwFileAttributes = (uint)dwFlagsAndAttributes & 0x0000FFFF;
            parameters.dwSecurityQosFlags = (uint)dwFlagsAndAttributes & SECURITY_VALID_SQOS_FLAGS;
            parameters.dwFileFlags = (uint)dwFlagsAndAttributes & 0xFFF00000;

            parameters.hTemplateFile = hTemplateFile;
            fixed (Interop.Kernel32.SECURITY_ATTRIBUTES* lpSecurityAttributes = &secAttrs)
            {
                parameters.lpSecurityAttributes = lpSecurityAttributes;
                return CreateNamedPipeClientPrivate(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref parameters);
            }
        }
    }
}
