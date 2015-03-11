// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32
{
    internal static class UnsafeNativeMethods
    {
        public const int
            FILE_LIST_DIRECTORY = (0x0001),
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            FILE_SHARE_DELETE = 0x00000004,
            OPEN_EXISTING = 3,
            FILE_FLAG_OVERLAPPED = 0x40000000,
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        [DllImport("api-ms-win-core-file-l2-1-0.dll", EntryPoint = "ReadDirectoryChangesW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public unsafe static extern bool ReadDirectoryChangesW(SafeFileHandle hDirectory, byte* lpBuffer,
                                                               int nBufferLength, int bWatchSubtree, int dwNotifyFilter, out int lpBytesReturned,
                                                               NativeOverlapped* lpOverlapped, IntPtr lpCompletionRoutine);
    }
}
