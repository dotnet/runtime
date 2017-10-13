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
        public void ClearLog()
        {
            string logName = "ClearTest";
            string source = "Source_" + nameof(ClearLog);

            if (!AdminHelpers.IsProcessElevated())
                return;

            try
            {
                EventLog.CreateEventSource(source, logName);
                using (EventLog myLog = new EventLog())
                {
                    myLog.Source = source;
                    myLog.Clear();
                    myLog.WriteEntry("Writing to event log.");
                    Assert.Equal(myLog.Entries.Count, 1);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(logName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ApplicationEventLog_Count()
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                Assert.InRange(eventLog.Entries.Count, 1, Int32.MaxValue);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void DeleteLog()
        {
            string logName = "DeleteTest";
            string source = "Source_" + nameof(DeleteLog);
            if (!AdminHelpers.IsProcessElevated())
                return;

            try
            {
                EventLog.CreateEventSource(source, logName);
                Assert.True(EventLog.Exists(logName));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(logName);
                Assert.False(EventLog.Exists(logName));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckLogName()
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                Assert.Equal("Application", eventLog.LogDisplayName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckMachineName()
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                Assert.Equal(".", eventLog.MachineName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetLogDisplayName_NotSet()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = Guid.NewGuid().ToString("N");
                Assert.Throws<InvalidOperationException>(() => eventLog.LogDisplayName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetLogDisplayName_Set()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = "Application";
                Assert.Equal("Application", eventLog.LogDisplayName);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EventLogs_Get()
        {
            EventLog[] eventLogCollection = EventLog.GetEventLogs();
            Assert.Contains(eventLogCollection, eventlog => eventlog.LogDisplayName.Equals("Application"));
            //Assert.Contains(eventLogCollection, eventlog => eventlog.LogDisplayName.Equals("Security"));
            //Assert.Contains(eventLogCollection, eventlog => eventlog.LogDisplayName.Equals("System"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetMaxKilobytes_Set()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = "Application";
                eventLog.MaximumKilobytes = 0x400;
                Assert.Equal(0x400, eventLog.MaximumKilobytes);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void MaxKilobytesOutOfRangeException()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = "Application";
                Assert.Throws<ArgumentOutOfRangeException>(() => eventLog.MaximumKilobytes = 2);
                Assert.Throws<ArgumentOutOfRangeException>(() => eventLog.MaximumKilobytes = 0x3FFFC1);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void OverflowAndRetention_Set()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = "Application";
                eventLog.ModifyOverflowPolicy(OverflowAction.DoNotOverwrite, 1);
                Assert.Equal(OverflowAction.DoNotOverwrite, eventLog.OverflowAction);
                Assert.Equal(-1, eventLog.MinimumRetentionDays);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void OverflowAndRetentionDaysOutOfRange()
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Log = "Application";
                Assert.Throws<ArgumentOutOfRangeException>(() => eventLog.ModifyOverflowPolicy(OverflowAction.OverwriteOlder, 400));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void MachineName_Set()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;
            using (EventLog myLog = new EventLog())
            {
                myLog.Log = "Application";
                myLog.MachineName = Environment.MachineName;
                string source = "Source_" + nameof(MachineName_Set);
                try
                {
                    EventLog.CreateEventSource(source, myLog.LogDisplayName);
                    Assert.True(EventLog.SourceExists(source, Environment.MachineName));
                }
                finally
                {
                    EventLog.DeleteEventSource(source);
                }

                Assert.False(EventLog.SourceExists(source, Environment.MachineName));
            }
        }
    }
}
