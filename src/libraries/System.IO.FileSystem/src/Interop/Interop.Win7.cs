// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    // This contains the subset of PInvokes that are needed to support pre-Win8 machines.
    // Those machines will require API-set dlls deployed seperately since they aren't inbox.
    internal static partial class mincore
    {
        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern SafeFileHandle CreateFile(string lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    [In] ref SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile);

        internal static int CopyFile(string src, string dst, bool failIfExists)
        {
            uint copyFlags = failIfExists ? Interop.COPY_FILE_FAIL_IF_EXISTS : 0;
            uint cancel = 0;
            if (!CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, copyFlags))
            {
                return Marshal.GetLastWin32Error();
            }

            return Interop.ERROR_SUCCESS;
        }

        [DllImport("api-ms-win-core-file-l2-1-0.dll", EntryPoint = "CopyFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool CopyFileEx(string src, string dst, IntPtr progressRoutine, IntPtr progressData, ref uint cancel, uint flags);

        [DllImport("api-ms-win-core-errorhandling-l1-1-0.dll", SetLastError = false, EntryPoint = "SetErrorMode", ExactSpelling = true)]
        internal static extern uint SetErrorMode(uint newMode);
    }
}
