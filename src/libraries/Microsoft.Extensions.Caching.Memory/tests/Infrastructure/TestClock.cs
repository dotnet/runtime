// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ISystemClockTemporalContext = Microsoft.Extensions.Internal.ClockQuantization.ISystemClockTemporalContext;

namespace Microsoft.Extensions.Internal
{
    public class TestClock : ISystemClock, ISystemClockTemporalContext
    {
        public TestClock()
        {
            UtcNow = new DateTime(2013, 6, 15, 12, 34, 56, 789);
        }

        public DateTimeOffset UtcNow { get; set; }

        public void Add(TimeSpan timeSpan)
        {
            UtcNow = UtcNow + timeSpan;
            _clockAdjusted?.Invoke(this, EventArgs.Empty);
        }

        TimeSpan? ISystemClockTemporalContext.MetronomeIntervalTimeSpan => null;

        bool ISystemClockTemporalContext.ClockIsManual { get; } = true;

        private EventHandler _clockAdjusted;
        event EventHandler ISystemClockTemporalContext.ClockAdjusted
        {
            add    => _clockAdjusted += value;
            remove => _clockAdjusted -= value;
        }

        private EventHandler _metronomeTicked;
        event EventHandler ISystemClockTemporalContext.MetronomeTicked
        {
            add    => _metronomeTicked += value;
            remove => _metronomeTicked -= value;
        }
    }
}
