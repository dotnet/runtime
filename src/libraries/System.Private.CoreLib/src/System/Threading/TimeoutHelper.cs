// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// A helper class to capture a start time using <see cref="Environment.TickCount"/> as a time in milliseconds.
    /// Also updates a given timeout by subtracting the current time from the start time.
    /// </summary>
    internal static class TimeoutHelper
    {
        /// <summary>
        /// Returns <see cref="Environment.TickCount"/> as a start time in milliseconds as a <see cref="UInt32"/>.
        /// <see cref="Environment.TickCount"/> rolls over from positive to negative every ~25 days, then ~25 days to back to positive again.
        /// <see cref="UInt32"/> is used to ignore the sign and double the range to 50 days.
        /// </summary>
        public static uint GetTime()
        {
            return (uint)Environment.TickCount;
        }

        /// <summary>
        /// Helper function to measure and update the elapsed time
        /// </summary>
        /// <param name="startTime"> The first time (in milliseconds) observed when the wait started</param>
        /// <param name="originalWaitMillisecondsTimeout">The original wait timeout in milliseconds</param>
        /// <returns>The new wait time in milliseconds, or -1 if the time expired</returns>
        public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
            // The function must be called in case the time out is not infinite
            Debug.Assert(originalWaitMillisecondsTimeout != Timeout.Infinite);

            uint elapsedMilliseconds = (GetTime() - startTime);

            // Check the elapsed milliseconds is greater than max int because this property is uint
            if (elapsedMilliseconds > int.MaxValue)
            {
                return 0;
            }

            // Subtract the elapsed time from the current wait time
            int currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }
    }
}
