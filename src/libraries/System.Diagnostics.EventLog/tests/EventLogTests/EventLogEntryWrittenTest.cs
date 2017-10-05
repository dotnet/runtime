// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogEntryEventWriitenTest
    {
        static AutoResetEvent signal;
        private string source = Guid.NewGuid().ToString("N");
        private string message = "EventLogEntryEventWrittenTestMessage";
        private string log = Guid.NewGuid().ToString("N");
        private static int eventCounter = 0;

        public void RaisingEvent(bool value = true)
        {
            signal = new AutoResetEvent(false);

            try
            {
                EventLog.CreateEventSource(source, log);
                using (EventLog myLog = new EventLog())
                {
                    myLog.Source = source;
                    myLog.EntryWritten += new EntryWrittenEventHandler(MyOnEntryWritten);
                    myLog.EnableRaisingEvents = value;
                    myLog.WriteEntry(message, EventLogEntryType.Information);
                    if (value)
                        signal.WaitOne();
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        public void MyOnEntryWritten(object source, EntryWrittenEventArgs e)
        {
            eventCounter += 1;
            signal.Set();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EntryWrittenEventTest()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;
            RaisingEvent();
            Assert.Equal(1, eventCounter);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EntryWrittenEventDisableTest()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;
            eventCounter = 0;
            RaisingEvent(false);
            Assert.Equal(0, eventCounter);
        }
    }
}
