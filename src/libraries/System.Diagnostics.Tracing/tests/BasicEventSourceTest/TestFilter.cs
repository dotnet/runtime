// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;

namespace BasicEventSourceTests
{
    public class TestFilter
    {
        /// <summary>
        /// Tests that Events with a level lower than the logging level will be filtered
        /// </summary>
        [Fact]
        public static void TestFilterEvent()
        {
            using (EventCountListener listener = new EventCountListener())
            using (TestSource log = new TestSource())
            {
                listener.EnableEvents(log, EventLevel.Error);

                /* This call should be filtered since it is at WarningLevel and we are logging Error */
                log.WarningEventWithArgs(1, 2, 3, false);

                /* This call should be filtered since it is at Verbose and we are logging Error */
                log.VerboseEvent();

                Assert.Equal(0, listener.EventCount);
            }
        }
    }

    /// <summary>
    /// EventListener that keeps track of the number of events that have been written.
    /// </summary>
    public class EventCountListener : EventListener
    {
        public int EventCount = 0;

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            EventCount++;
        }
    }

    public class TestSource : EventSource
    {
        [Event(9000, Level = EventLevel.Warning)]
        public void WarningEventWithArgs(int i, int j, int k, bool b)
        {
            WriteEvent(9000, i, j, k, b);
        }

        [Event(9001, Level = EventLevel.Verbose)]
        public void VerboseEvent()
        {
            WriteEvent(9001);
        }
    }
}
