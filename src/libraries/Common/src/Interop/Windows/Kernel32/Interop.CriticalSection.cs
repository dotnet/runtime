// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static unsafe partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CRITICAL_SECTION
        {
            private IntPtr DebugInfo;
            private int LockCount;
            private int RecursionCount;
            private IntPtr OwningThread;
            private IntPtr LockSemaphore;
            private UIntPtr SpinCount;
        }

        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern void InitializeCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern void EnterCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern void LeaveCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern void DeleteCriticalSection(CRITICAL_SECTION* lpCriticalSection);
    }
}
