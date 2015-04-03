// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern int getpriority(PriorityWhich which, int who);

        internal static int getpriority(PriorityWhich which, int who, out int priority)
        {
            priority = getpriority(which, who);
            return Marshal.GetLastWin32Error();
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int setpriority(PriorityWhich which, int who, int prio);

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
