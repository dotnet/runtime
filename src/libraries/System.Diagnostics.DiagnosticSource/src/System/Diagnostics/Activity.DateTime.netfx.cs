// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private class TimeSync
        {
            public readonly DateTime SyncUtcNow = DateTime.UtcNow;
            public readonly long SyncStopwatchTicks = Stopwatch.GetTimestamp();
        }

        private static TimeSync timeSync = new TimeSync();

        // sync DateTime and Stopwatch ticks every 2 hours
        private static Timer syncTimeUpdater = new Timer(s => { Sync(); }, null, 0, 7200000);
    }
}