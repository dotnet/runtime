// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogEntryEventWrittenTest
    {
        static AutoResetEvent signal;
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
                using (EventLog myLog = new EventLog())
                {
                    myLog.Source = source;
                    myLog.EntryWritten += new EntryWrittenEventHandler((object sourceObject, EntryWrittenEventArgs e) =>
                    {
                        eventCounter += 1;
                        signal.Set();
                    });
                    myLog.EnableRaisingEvents = waitOnEvent;
                    myLog.WriteEntry(message, EventLogEntryType.Information);
                    if (waitOnEvent)
                        signal.WaitOne();
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void EntryWrittenEventRaised()
        {
            RaisingEvent("EnableEvent", nameof(EntryWrittenEventRaised));
            Assert.Equal(1, eventCounter);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void EntryWrittenEventRaiseDisable()
        {
            RaisingEvent("DisableEvent", nameof(EntryWrittenEventRaiseDisable), waitOnEvent: false);
            Assert.Equal(0, eventCounter);
        }
    }
}
