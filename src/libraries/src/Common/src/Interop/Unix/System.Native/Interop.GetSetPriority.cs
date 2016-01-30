// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum PriorityWhich : int
        {
            PRIO_PROCESS    = 0,
            PRIO_PGRP       = 1,
            PRIO_USER       = 2,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPriority", SetLastError = true)]
        private static extern int GetPriority(PriorityWhich which, int who);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPriority", SetLastError = true)]
        internal static extern int SetPriority(PriorityWhich which, int who, int nice);

        /// <summary>
        /// Wrapper around getpriority since getpriority can return from -20 to 20; therefore,
        /// we cannot rely on the return value for success and failure. This wrapper makes the
        /// getpriority call to act more naturally where the return value is the actual error
        /// value (or 0 if success) instead of forcing the caller to retrieve the last error.
        /// </summary>
        /// <returns>Returns 0 on success; otherwise, returns the errno value</returns>
        internal static int GetPriority(PriorityWhich which, int who, out int priority)
        {
            priority = GetPriority(which, who);
            return Marshal.GetLastWin32Error();
        }

        internal static System.Diagnostics.ThreadPriorityLevel GetThreadPriorityFromNiceValue(int nice)
        {
            Debug.Assert((nice >= -20) && (nice <= 20));
            return
                (nice < -15) ? ThreadPriorityLevel.TimeCritical :
                (nice < -10) ? ThreadPriorityLevel.Highest :
                (nice < -5)  ? ThreadPriorityLevel.AboveNormal :
                (nice == 0)  ? ThreadPriorityLevel.Normal :
                (nice <= 5)  ? ThreadPriorityLevel.BelowNormal :
                (nice <= 10) ? ThreadPriorityLevel.Lowest :
                ThreadPriorityLevel.Idle;
        }

        internal static int GetNiceValueFromThreadPriority(System.Diagnostics.ThreadPriorityLevel priority)
        {
            return (priority == ThreadPriorityLevel.TimeCritical ? -20 :
                priority == ThreadPriorityLevel.Highest ? -15 :
                priority == ThreadPriorityLevel.AboveNormal ? -10 :
                priority == ThreadPriorityLevel.Normal ? 0 :
                priority == ThreadPriorityLevel.BelowNormal ? 5 :
                priority == ThreadPriorityLevel.Lowest ? 10 :
                20);
        }
    }
}
