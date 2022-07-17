// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CONDITION_VARIABLE
        {
            private IntPtr Ptr;
        }

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void InitializeConditionVariable(CONDITION_VARIABLE* ConditionVariable);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void WakeConditionVariable(CONDITION_VARIABLE* ConditionVariable);

        [LibraryImport(Libraries.Kernel32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SleepConditionVariableCS(CONDITION_VARIABLE* ConditionVariable, CRITICAL_SECTION* CriticalSection, int dwMilliseconds);
    }
}
