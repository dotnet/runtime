// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern void InitializeConditionVariable(out CONDITION_VARIABLE ConditionVariable);

        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern void WakeConditionVariable(ref CONDITION_VARIABLE ConditionVariable);

        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SleepConditionVariableCS(ref CONDITION_VARIABLE ConditionVariable, ref CRITICAL_SECTION CriticalSection, int dwMilliseconds);
    }
}
