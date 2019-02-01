// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogReaderTests
    {
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData("Application", false)]
        [InlineData("Application", true)]
        public void ReadEvent(string logName, bool useQuery)
        {
            var eventLog =
                useQuery
                ? new EventLogReader(
                     new EventLogQuery(logName, PathType.LogName) { ReverseDirection = true })
                : new EventLogReader(logName);

            using (eventLog)
            {
                using (EventRecord record = eventLog.ReadEvent())
                {
                    Assert.NotNull(record);
                    Assert.Equal(logName, record.LogName);
                }
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData("Microsoft-Windows-PowerShell/Operational", false)]
        [InlineData("Microsoft-Windows-PowerShell/Operational", true)]
        public void ReadEventPsh(string logName, bool useQuery)
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;

            ReadEvent(logName, useQuery);
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void WrongPathType_ReverseDirection_Throws(bool useBookmark)
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery(null, PathType.FilePath, "*[System[(Level=2)]]") { ReverseDirection = true };
            if (useBookmark)
            {
                Assert.Throws<EventLogException>(() => new EventLogReader(query, bookmark: null));
                Assert.Throws<EventLogException>(() => new EventLogReader(query, bookmark: Helpers.GetBookmark("Application", PathType.LogName)));
            }
            else
            {
                Assert.Throws<EventLogException>(() => new EventLogReader(query));
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void WrongPathType_TolerateQueryErrors_Throws(bool useBookmark)
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery(null, PathType.FilePath, "*[System[(Level=2)]]") { TolerateQueryErrors = true };
            if (useBookmark)
            {
                Assert.Throws<EventLogException>(() => new EventLogReader(query, bookmark: null));
                Assert.Throws<EventLogException>(() => new EventLogReader(query, bookmark: Helpers.GetBookmark("Application", PathType.LogName)));
            }
            else
            {
                Assert.Throws<EventLogException>(() => new EventLogReader(query));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void CastToEventLogRecord_NotNull()
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, Helpers.GetBookmark("Application", PathType.LogName));
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    Assert.NotNull(record);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Seek()
        {
            using (var eventLog = new EventLogReader("Application"))
            {
                using (EventRecord record = eventLog.ReadEvent())
                {
                    eventLog.Seek(record.Bookmark);
                    eventLog.Seek(IO.SeekOrigin.Begin, 0);
                    eventLog.Seek(IO.SeekOrigin.Current, 0);
                    eventLog.Seek(IO.SeekOrigin.End, 0);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void CancelReading()
        {
            using (var eventLog = new EventLogReader("Application"))
            {
                eventLog.CancelReading();
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void LogStatus()
        {
            using (var eventLog = new EventLogReader("Application"))
            {
                Assert.NotEmpty(eventLog.LogStatus);
                foreach (var logStatus in eventLog.LogStatus)
                {
                    Assert.Equal("Application", logStatus.LogName);
                    Assert.Equal(0, logStatus.StatusCode);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void BatchSize_OtherCtor()
        {
            using (var eventLog = new EventLogReader("Application", PathType.LogName))
            {
                Assert.Equal(64, eventLog.BatchSize);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Ctor_NullQuery_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EventLogReader(null, null));
        }
    }
}

