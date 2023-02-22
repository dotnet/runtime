// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
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

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void InitializeCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void EnterCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void LeaveCriticalSection(CRITICAL_SECTION* lpCriticalSection);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void DeleteCriticalSection(CRITICAL_SECTION* lpCriticalSection);
    }
}
