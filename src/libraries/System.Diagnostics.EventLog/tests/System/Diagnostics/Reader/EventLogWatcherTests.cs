// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogWatcherTests
    {
        private static AutoResetEvent signal;
        private const string message = "EventRecordWrittenTestMessage";
        private int eventCounter;

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Ctor_Default()
        {
            using (var eventLogWatcher = new EventLogWatcher("Application"))
            {
                Assert.False(eventLogWatcher.Enabled);
                eventLogWatcher.Enabled = true;
                Assert.True(eventLogWatcher.Enabled);
                eventLogWatcher.Enabled = false;
                Assert.False(eventLogWatcher.Enabled);
            }
        }
            
        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Ctor_UsingBookmark()
        {
            EventBookmark bookmark = GetBookmark();
            Assert.Throws<ArgumentNullException>(() => new EventLogWatcher(null, bookmark, true));
            Assert.Throws<InvalidOperationException>(() => new EventLogWatcher(new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true }, bookmark, true));

            var query = new EventLogQuery("Application", PathType.LogName, "*[System]");
            using (var eventLogWatcher = new EventLogWatcher(query, bookmark))
            {
                Assert.False(eventLogWatcher.Enabled);
                eventLogWatcher.Enabled = true;
                Assert.True(eventLogWatcher.Enabled);
                eventLogWatcher.Enabled = false;
                Assert.False(eventLogWatcher.Enabled);
            }
        }

        private EventBookmark GetBookmark()
        {
            EventBookmark bookmark;
            EventLogQuery eventLogQuery = new EventLogQuery("Application", PathType.LogName, "*[System]");
            using (var eventLog = new EventLogReader(eventLogQuery))
            using (var record = eventLog.ReadEvent())
            {
                Assert.NotNull(record);
                bookmark = record.Bookmark;
                Assert.NotNull(record.Bookmark);
            }
            return bookmark;
        }

        public void RaisingEvent(string log, string methodName, bool waitOnEvent = true)
        {
            signal = new AutoResetEvent(false);
            eventCounter = 0;
            string source = "Source_" + methodName;

            try
            {
                EventLog.CreateEventSource(source, log);
                var query = new EventLogQuery(log, PathType.LogName);
                using (EventLog eventLog = new EventLog())
                using (EventLogWatcher eventLogWatcher = new EventLogWatcher(query))
                {
                    eventLog.Source = source;
                    eventLogWatcher.EventRecordWritten += (s, e) =>
                    {
                        eventCounter += 1;
                        Assert.True(e.EventException != null || e.EventRecord != null);
                        signal.Set();
                    };
                    Helpers.RetryOnWin7(() => eventLogWatcher.Enabled = waitOnEvent);
                    Helpers.RetryOnWin7(() => eventLog.WriteEntry(message, EventLogEntryType.Information));
                    if (waitOnEvent)
                    {
                        Assert.True(signal.WaitOne(6000));
                    }
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void RecordWrittenEventRaised()
        {
            RaisingEvent("EnableEvent", nameof(RecordWrittenEventRaised));
            Assert.NotEqual(0, eventCounter);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void RecordWrittenEventRaiseDisable()
        {
            RaisingEvent("DisableEvent", nameof(RecordWrittenEventRaiseDisable), waitOnEvent: false);
            Assert.Equal(0, eventCounter);
        }
    }
}
