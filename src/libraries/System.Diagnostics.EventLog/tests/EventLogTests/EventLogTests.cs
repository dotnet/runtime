// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EventLogReinitializationException()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.BeginInit();
                Assert.Throws<InvalidOperationException>(() => eventLog.BeginInit());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ClearLogTest()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            string source = Guid.NewGuid().ToString("N");
            string logName = Guid.NewGuid().ToString("N");
            try
            {
                EventLog.CreateEventSource(source, logName);
                using (EventLog myLog = new EventLog())
                {
                    myLog.Source = source;
                    myLog.WriteEntry("Writing to event log.");
                    Assert.True(myLog.Entries.Count != 0);
                    myLog.Clear();
                    Assert.Equal(0, myLog.Entries.Count);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ApplicationEventLog_Count()
        {
            using ( EventLog ael = new EventLog("Application"))
            { 
                Assert.InRange(ael.Entries.Count, 1, Int32.MaxValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void DeleteLogTest()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            string source = Guid.NewGuid().ToString("N");
            string logName = Guid.NewGuid().ToString("N");
            try
            {
                EventLog.CreateEventSource(source, logName);
                Assert.True(EventLog.Exists(logName));
            }
            finally
            {
                EventLog.Delete(logName);
                Assert.False(EventLog.Exists(logName));
            }
        }
    }
}