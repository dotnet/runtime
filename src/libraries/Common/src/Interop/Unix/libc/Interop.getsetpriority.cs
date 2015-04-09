// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

internal static partial class Interop
{
    internal static partial class libc
    {
        /// <summary>
        /// Gets the priority (nice value) of a certain execution group
        /// </summary>
        /// <param name="which">The execution group to retrieve the nice value for</param>
        /// <param name="who">The id of the group</param>
        /// <returns>
        /// Returns the nice value (from -20 to 20) of the group on success; on failure,
        /// returns -1. To actually determine success or failure, clear errno before calling 
        /// (which should be done automatically by the runtime) and check errno after the call.
        /// </returns>
        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern int getpriority(PriorityWhich which, int who);

        /// <summary>
        /// Wrapper around getpriority since getpriority can return from -20 to 20; therefore,
        /// we cannot rely on the return value for success and failure. This wrapper makes the
        /// getpriority call to act more naturally where the return value is the actual error
        /// value (or 0 if success) instead of forcing the caller to retrieve the last error.
        /// </summary>
        /// <param name="which"></param>
        /// <param name="who"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        internal static int getpriority(PriorityWhich which, int who, out int priority)
        {
            priority = getpriority(which, who);
            return Marshal.GetLastWin32Error();
        }

        /// <summary>
        /// Sets the priority (nice value) of the specified execution group.
        /// </summary>
        /// <param name="which">The execution group to change the priority of</param>
        /// <param name="who">The ID of the group</param>
        /// <param name="prio">The new priority</param>
        /// <returns>Returns 0 on success, -1 on failure</returns>
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
