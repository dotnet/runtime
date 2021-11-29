// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Diagnostics.Tests
{
    public class EventLogRecordTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void GetPropertyValues()
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, Helpers.GetBookmark("Application", PathType.LogName));
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    Assert.Throws<ArgumentNullException>(() => record.GetPropertyValues(null));
                    Assert.NotNull(record.GetPropertyValues(new EventLogPropertySelector(new [] {"dummy"})));
                }
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/28403")]
        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData("System")]
        [InlineData("Application")]
        public void FormatDescription(string log)
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery(log, PathType.LogName, "*[System]") { ReverseDirection = true };
            using (var eventLog = new EventLogReader(query, Helpers.GetBookmark(log, PathType.LogName)))
            {
                using (EventRecord record = eventLog.ReadEvent())
                {
                    Assert.IsType<EventLogRecord>(record);
                    string description = record.FormatDescription();
                    Assert.Equal(description, record.FormatDescription(null));
                    Assert.Equal(description, record.FormatDescription(new List<object>()));
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Properties()
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, Helpers.GetBookmark("Application", PathType.LogName));
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    Assert.NotNull(record.Properties);
                    foreach (EventProperty eventProperty in record.Properties)
                    {
                        Assert.NotNull(eventProperty.Value);
                    }
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void ToXml()
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;
            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, Helpers.GetBookmark("Application", PathType.LogName));
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    Assert.NotNull(record.ToXml());
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void ExceptionOnce()
        {
            if (PlatformDetection.IsWindows7 ||  // Null events in PowerShell log
                PlatformDetection.IsWindows10Version22000OrGreater) // ActiveIssue("https://github.com/dotnet/runtime/issues/58829")
                return;
            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, Helpers.GetBookmark("Application", PathType.LogName));
            string levelDisplayName = null, opcodeDisplayName = null, taskDisplayName = null;
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    ThrowsMaxOnce<EventLogNotFoundException>(() => levelDisplayName = record.LevelDisplayName);
                    ThrowsMaxOnce<EventLogNotFoundException>(() => opcodeDisplayName = record.OpcodeDisplayName);
                    ThrowsMaxOnce<EventLogNotFoundException>(() => taskDisplayName = record.TaskDisplayName);
                    Assert.Equal(levelDisplayName, record.LevelDisplayName);
                    Assert.Equal(opcodeDisplayName, record.OpcodeDisplayName);
                    Assert.Equal(taskDisplayName, record.TaskDisplayName);
                }
            }
        }

        private void ThrowsMaxOnce<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                action();
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void EventLogRecord_CheckProperties_RemainSame()
        {
            if (PlatformDetection.IsWindows7) // Null events in PowerShell log
                return;

            SecurityIdentifier userId;
            byte? version, level;
            short? opcode;
            Guid? providerId, activityId, relatedActivityId;
            int? processId, threadId, qualifiers, task;
            long? keywords, recordId;
            string providerName, machineName, containerLog;
            DateTime? timeCreated;
            IEnumerable<int> matchedQueryIds;
            EventBookmark bookmark, bookmarkArg = Helpers.GetBookmark("Application", PathType.LogName);

            var query = new EventLogQuery("Application", PathType.LogName, "*[System]") { ReverseDirection = true };
            var eventLog = new EventLogReader(query, bookmarkArg);
            using (eventLog)
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    userId = record.UserId;
                    version = record.Version;
                    opcode = record.Opcode;
                    providerId = record.ProviderId;
                    processId = record.ProcessId;
                    recordId = record.RecordId;
                    threadId = record.ThreadId;
                    qualifiers = record.Qualifiers;
                    level = record.Level;
                    keywords = record.Keywords;
                    task = record.Task;
                    providerName = record.ProviderName;
                    machineName = record.MachineName;
                    timeCreated = record.TimeCreated;
                    containerLog = record.ContainerLog;
                    matchedQueryIds = record.MatchedQueryIds;
                    activityId = record.ActivityId;
                    relatedActivityId = record.RelatedActivityId;
                    bookmark = record.Bookmark;
                }
            }

            using (eventLog = new EventLogReader(query, bookmarkArg))
            {
                using (var record = (EventLogRecord)eventLog.ReadEvent())
                {
                    Assert.Equal(userId, record.UserId);
                    Assert.Equal(version, record.Version);
                    Assert.Equal(opcode, record.Opcode);
                    Assert.Equal(providerId, record.ProviderId);
                    Assert.Equal(processId, record.ProcessId);
                    Assert.Equal(recordId, record.RecordId);
                    Assert.Equal(threadId, record.ThreadId);
                    Assert.Equal(qualifiers, record.Qualifiers);
                    Assert.Equal(level, record.Level);
                    Assert.Equal(keywords, record.Keywords);
                    Assert.Equal(task, record.Task);
                    Assert.Equal(providerName, record.ProviderName);
                    Assert.Equal(machineName, record.MachineName);
                    Assert.Equal(timeCreated, record.TimeCreated);
                    Assert.Equal(containerLog, record.ContainerLog);
                    Assert.Equal(matchedQueryIds, record.MatchedQueryIds);
                    Assert.Equal(activityId, record.ActivityId);
                    Assert.Equal(relatedActivityId, record.RelatedActivityId);
                    Assert.NotNull(record.Bookmark);
                    Assert.NotEqual(bookmark, record.Bookmark);
                }
            }
        }
    }
}
