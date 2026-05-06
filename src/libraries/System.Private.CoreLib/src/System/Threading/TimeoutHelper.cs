// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// A helper class to update a given timeout by subtracting the current time from the start time.
    /// </summary>
    internal static class TimeoutHelper
    {
        /// <summary>
        /// Helper function to measure and update the elapsed time
        /// </summary>
        /// <param name="startTime"> The first time (in milliseconds) observed when the wait started</param>
        /// <param name="originalWaitMillisecondsTimeout">The original wait timeout in milliseconds</param>
        /// <returns>The new wait time in milliseconds</returns>
        public static long UpdateTimeOut(long startTime, long originalWaitMillisecondsTimeout)
        {
            // The function must be called in case the time out is not infinite
            Debug.Assert(originalWaitMillisecondsTimeout != Timeout.Infinite);

            ulong elapsedMilliseconds = (ulong)(Environment.TickCount64 - startTime);

            if (elapsedMilliseconds > long.MaxValue)
            {
                return 0;
            }

            long currentWaitTimeout = originalWaitMillisecondsTimeout - (long)elapsedMilliseconds;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }
    }
}
