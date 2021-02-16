// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        [DllImport(Libraries.Kernel32)]
        internal static extern uint WaitForMultipleObjectsEx(uint nCount, IntPtr lpHandles, BOOL bWaitAll, uint dwMilliseconds, BOOL bAlertable);

        [DllImport(Libraries.Kernel32)]
        internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport(Libraries.Kernel32)]
        internal static extern uint SignalObjectAndWait(IntPtr hObjectToSignal, IntPtr hObjectToWaitOn, uint dwMilliseconds, BOOL bAlertable);

        [DllImport(Libraries.Kernel32)]
        internal static extern void Sleep(uint milliseconds);

        internal const uint CREATE_SUSPENDED = 0x00000004;
        internal const uint STACK_SIZE_PARAM_IS_A_RESERVATION = 0x00010000;

        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe SafeWaitHandle CreateThread(
            IntPtr lpThreadAttributes,
            IntPtr dwStackSize,
            delegate* unmanaged<IntPtr, uint> lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        [DllImport(Libraries.Kernel32)]
        internal static extern uint ResumeThread(SafeWaitHandle hThread);

        [DllImport(Libraries.Kernel32)]
        internal static extern IntPtr GetCurrentThread();

        internal const int DUPLICATE_SAME_ACCESS = 2;

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out SafeWaitHandle lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
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

        [DllImport(Libraries.Kernel32)]
        internal static extern ThreadPriority GetThreadPriority(SafeWaitHandle hThread);

        [DllImport(Libraries.Kernel32)]
        internal static extern bool SetThreadPriority(SafeWaitHandle hThread, int nPriority);
    }
}
