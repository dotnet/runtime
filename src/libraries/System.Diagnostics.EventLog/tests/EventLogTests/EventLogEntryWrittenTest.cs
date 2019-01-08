// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogEntryEventWrittenTest
    {
        private static AutoResetEvent signal;
        private const string message = "EventLogEntryEventWrittenTestMessage";
        private int eventCounter;

        public void RaisingEvent(string log, string methodName, bool waitOnEvent = true)
        {
            signal = new AutoResetEvent(false);
            eventCounter = 0;
            string source = "Source_" + methodName;

            try
            {
                EventLog.CreateEventSource(source, log);
                using (EventLog eventLog = new EventLog())
                {
                    eventLog.Source = source;
                    eventLog.EntryWritten += new EntryWrittenEventHandler((object sourceObject, EntryWrittenEventArgs e) =>
                    {
                        eventCounter += 1;
                        signal.Set();
                    });
                    Helpers.RetryOnWin7(() => eventLog.EnableRaisingEvents = waitOnEvent);
                    Helpers.RetryOnWin7(() => eventLog.WriteEntry(message, EventLogEntryType.Information));
                    if (waitOnEvent)
                    {
                        if (!signal.WaitOne(6000))
                        {
                            eventLog.WriteEntry(message, EventLogEntryType.Information);
                            Assert.True(signal.WaitOne(6000));
                        }
                        // The system responds to WriteEntry only if the last write event occurred at least six seconds previously.
                        // This implies that the system will only receive one EntryWritten event notification within a six-second interval, even if more than one event log change occurs.
                        // For more information https://msdn.microsoft.com/en-us/library/system.diagnostics.eventlog.entrywritten(v=vs.110).aspx
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
        public void EntryWrittenEventRaised()
        {
            RaisingEvent("EnableEvent", nameof(EntryWrittenEventRaised));
            Assert.NotEqual(0, eventCounter);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void EntryWrittenEventRaiseDisable()
        {
            RaisingEvent("DisableEvent", nameof(EntryWrittenEventRaiseDisable), waitOnEvent: false);
            Assert.Equal(0, eventCounter);
        }
    }
}
