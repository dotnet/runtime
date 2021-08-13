// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics
{
    partial class Activity
    {
        /// <summary>
        /// Returns high resolution (1 DateTime tick) current UTC DateTime.
        /// </summary>
        internal static DateTime GetUtcNow()
        {
            // DateTime.UtcNow accuracy on .NET Framework is ~16ms, this method
            // uses combination of Stopwatch and DateTime to calculate accurate UtcNow.

            var tmp = timeSync;

            // Timer ticks need to be converted to DateTime ticks
            long dateTimeTicksDiff = (long)((Stopwatch.GetTimestamp() - tmp.SyncStopwatchTicks) * 10000000L /
                                            (double)Stopwatch.Frequency);

            // DateTime.AddSeconds (or Milliseconds) rounds value to 1 ms, use AddTicks to prevent it
            return tmp.SyncUtcNow.AddTicks(dateTimeTicksDiff);
        }

        private static void Sync()
        {
            // wait for DateTime.UtcNow update to the next granular value
            Thread.Sleep(1);
            timeSync = new TimeSync();
        }

        private sealed class TimeSync
        {
            public readonly DateTime SyncUtcNow = DateTime.UtcNow;
            public readonly long SyncStopwatchTicks = Stopwatch.GetTimestamp();
        }

        private static TimeSync timeSync = new TimeSync();

        // sync DateTime and Stopwatch ticks every 2 hours
#pragma warning disable CA1823 // suppress unused field warning, as it's used to keep the timer alive
        private static readonly Timer syncTimeUpdater = InitalizeSyncTimer();
#pragma warning restore CA1823

        [System.Security.SecuritySafeCritical]
        private static Timer InitalizeSyncTimer()
        {
            Timer timer;
            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                timer = new Timer(s => { Sync(); }, null, 0, 7200000);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                    ExecutionContext.RestoreFlow();
            }

            return timer;
        }
    }
}
