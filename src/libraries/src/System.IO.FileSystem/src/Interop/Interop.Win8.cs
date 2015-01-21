// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal struct COPYFILE2_EXTENDED_PARAMETERS
    {
        public uint dwSize;
        public uint dwCopyFlags;
        public IntPtr pfCancel;
        public IntPtr pProgressRoutine;
        public IntPtr pvCallbackContext;
    }

    internal struct CREATEFILE2_EXTENDED_PARAMETERS
    {
        public uint dwSize;
        public uint dwFileAttributes;
        public uint dwFileFlags;
        public uint dwSecurityQosFlags;
        public IntPtr lpSecurityAttributes;
        public IntPtr hTemplateFile;
    }

    internal static partial class mincore
    {
        private static unsafe SafeFileHandle CreateFile(String lpFileName,
                int dwDesiredAccess, System.IO.FileShare dwShareMode,
                ref SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            Interop.CREATEFILE2_EXTENDED_PARAMETERS parameters;
            parameters.dwSize = (uint)Marshal.SizeOf<Interop.CREATEFILE2_EXTENDED_PARAMETERS>();

            parameters.dwFileAttributes = (uint)dwFlagsAndAttributes & 0x0000FFFF;
            parameters.dwSecurityQosFlags = (uint)dwFlagsAndAttributes & 0x000F0000;
            parameters.dwFileFlags = (uint)dwFlagsAndAttributes & 0xFFF00000;

            parameters.hTemplateFile = hTemplateFile;
            fixed (SECURITY_ATTRIBUTES* lpSecurityAttributes = &securityAttrs)
            {
                parameters.lpSecurityAttributes = (IntPtr)lpSecurityAttributes;
                return CreateFile2(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref parameters);
            }
        }

        [DllImport("api-ms-win-core-file-l1-2-0.dll", EntryPoint = "CreateFile2", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern SafeFileHandle CreateFile2(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode, System.IO.FileMode dwCreationDisposition,
                    [In] ref Interop.CREATEFILE2_EXTENDED_PARAMETERS parameters);

        internal static int CopyFile(String src, String dst, bool failIfExists)
        {
            uint copyFlags = failIfExists ? Interop.COPY_FILE_FAIL_IF_EXISTS : 0;
            Interop.COPYFILE2_EXTENDED_PARAMETERS parameters = new Interop.COPYFILE2_EXTENDED_PARAMETERS()
            {
                dwSize = (uint)Marshal.SizeOf<Interop.COPYFILE2_EXTENDED_PARAMETERS>(),
                dwCopyFlags = copyFlags
            };

            int hr = CopyFile2(src, dst, ref parameters);

            return Win32Marshal.TryMakeWin32ErrorCodeFromHR(hr);
        }

        [DllImport("api-ms-win-core-file-l2-1-0.dll", CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern int CopyFile2(string pwszExistingFileName, string pwszNewFileName, ref COPYFILE2_EXTENDED_PARAMETERS pExtendedParameters);

        internal static uint SetErrorMode(uint uMode)
        {
            // Prompting behavior no longer occurs in all platforms supported
            return 0;
        }
    }
}
