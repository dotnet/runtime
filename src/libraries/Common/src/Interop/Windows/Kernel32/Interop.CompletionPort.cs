// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, UIntPtr CompletionKey, int NumberOfConcurrentThreads);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostQueuedCompletionStatus(IntPtr CompletionPort, uint dwNumberOfBytesTransferred, UIntPtr CompletionKey, IntPtr lpOverlapped);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetQueuedCompletionStatus(IntPtr CompletionPort, out uint lpNumberOfBytesTransferred, out UIntPtr CompletionKey, out IntPtr lpOverlapped, int dwMilliseconds);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetQueuedCompletionStatusEx(
            IntPtr CompletionPort,
            OVERLAPPED_ENTRY* lpCompletionPortEntries,
            int ulCount,
            out int ulNumEntriesRemoved,
            int dwMilliseconds,
            [MarshalAs(UnmanagedType.Bool)] bool fAlertable);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct OVERLAPPED_ENTRY
        {
            public UIntPtr lpCompletionKey;
            public NativeOverlapped* lpOverlapped;
            public UIntPtr Internal;
            public uint dwNumberOfBytesTransferred;
        }
    }
}
