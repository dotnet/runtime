// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    internal sealed partial class CounterGroup
    {
        private static Thread? s_pollingThread;
        // Used for sleeping for a certain amount of time while allowing the thread to be woken up
        private static AutoResetEvent? s_pollingThreadSleepEvent;

        private static void CreatePollingTimer()
        {
            // Create the polling thread and init all the shared state if needed
            if (s_pollingThread == null)
            {
                s_pollingThreadSleepEvent = new AutoResetEvent(false);
                s_counterGroupEnabledList = new List<CounterGroup>();
                s_pollingThread = new Thread(PollForValues)
                {
                    IsBackground = true,
                    Name = ".NET Counter Poller"
                };
                s_pollingThread.Start();
            }
            else
            {
                // notify the polling thread that the polling interval may have changed and the sleep should be recomputed
                s_pollingThreadSleepEvent!.Set();
            }
        }

        private static void PollForValues()
        {
            AutoResetEvent? sleepEvent = null;
            lock (s_counterGroupLock)
            {
                sleepEvent = s_pollingThreadSleepEvent;
            }

            while (true)
            {
                var sleepDurationInMilliseconds = PollOnce();

                sleepEvent?.WaitOne(sleepDurationInMilliseconds);
            }
        }
    }
}
