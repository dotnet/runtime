// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    internal sealed partial class CounterGroup
    {
        private static Timer? s_pollingTimer;

        private static void CreatePollingTimer()
        {
            if (s_pollingTimer == null)
            {
                s_pollingTimer = new Timer(PollForValues, null, 0, 0);
                s_counterGroupEnabledList = new List<CounterGroup>();
            }
            else
            {
                // notify the polling callback that the polling interval may have changed and the sleep should be recomputed
                s_pollingTimer.Change(0, 0);
            }
        }

        private static void PollForValues(object? state)
        {
            var sleepDurationInMilliseconds = PollOnce();
            s_pollingTimer!.Change(sleepDurationInMilliseconds, 0);
        }
    }
}
