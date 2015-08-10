// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L2, EntryPoint = "ReadDirectoryChangesW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern bool ReadDirectoryChangesW(
            SafeFileHandle hDirectory, 
            byte[] lpBuffer, 
            int nBufferLength, 
            [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree, 
            int dwNotifyFilter, 
            out int lpBytesReturned,
            NativeOverlapped* lpOverlapped, 
            IntPtr lpCompletionRoutine);
    }
}
