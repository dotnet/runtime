// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, UIntPtr CompletionKey, int NumberOfConcurrentThreads);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostQueuedCompletionStatus(IntPtr CompletionPort, int dwNumberOfBytesTransferred, UIntPtr CompletionKey, IntPtr lpOverlapped);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetQueuedCompletionStatus(IntPtr CompletionPort, out int lpNumberOfBytes, out UIntPtr CompletionKey, out IntPtr lpOverlapped, int dwMilliseconds);
    }
}
