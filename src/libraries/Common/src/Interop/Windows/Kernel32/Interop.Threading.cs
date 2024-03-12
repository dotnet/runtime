// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial uint WaitForMultipleObjectsEx(uint nCount, IntPtr lpHandles, BOOL bWaitAll, uint dwMilliseconds, BOOL bAlertable);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial uint SignalObjectAndWait(IntPtr hObjectToSignal, IntPtr hObjectToWaitOn, uint dwMilliseconds, BOOL bAlertable);

        internal const uint CREATE_SUSPENDED = 0x00000004;
        internal const uint STACK_SIZE_PARAM_IS_A_RESERVATION = 0x00010000;

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial SafeWaitHandle CreateThread(
            IntPtr lpThreadAttributes,
            IntPtr dwStackSize,
            delegate* unmanaged<IntPtr, uint> lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial uint ResumeThread(SafeWaitHandle hThread);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial IntPtr GetCurrentThread();

        internal const int DUPLICATE_SAME_ACCESS = 2;

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out SafeWaitHandle lpTargetHandle,
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint dwOptions);

        internal enum ThreadPriority : int
        {
            Idle = -15,
            Lowest = -2,
            BelowNormal = -1,
            Normal = 0,
            AboveNormal = 1,
            Highest = 2,
            TimeCritical = 15,

            ErrorReturn = 0x7FFFFFFF
        }

        [LibraryImport(Libraries.Kernel32)]
        internal static partial ThreadPriority GetThreadPriority(SafeWaitHandle hThread);

        [LibraryImport(Libraries.Kernel32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetThreadPriority(SafeWaitHandle hThread, int nPriority);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetThreadIOPendingFlag(nint hThread, out BOOL lpIOIsPending);
    }
}
