// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Security;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogSessionTests : FileCleanupTestBase
    {
        private const string LogName = "Application";

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctors_ProviderNames_LogNames_NotEmpty(bool usingDefaultCtor)
        {
            using (var session = usingDefaultCtor ? new EventLogSession() : new EventLogSession(null))
            {
                Assert.NotEmpty(session.GetProviderNames());
                Assert.NotEmpty(session.GetLogNames());
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void ExportLogAndMessages_NullPath_Throws(bool usingDefaultCtor)
        {
            using (var session = usingDefaultCtor ? new EventLogSession() : new EventLogSession(null))
            {
                Assert.Throws<ArgumentNullException>(() => session.ExportLogAndMessages(null, PathType.LogName, LogName, GetTestFilePath()));
                // Does not throw:
                session.ExportLogAndMessages(LogName, PathType.LogName, LogName, GetTestFilePath());
                session.ExportLogAndMessages(LogName, PathType.LogName, LogName, GetTestFilePath(), false, targetCultureInfo: CultureInfo.CurrentCulture);
                session.CancelCurrentOperations();
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void ExportLog_InvalidInputCombinations_Throws(bool usingDefaultCtor)
        {
            using (var session = usingDefaultCtor ? new EventLogSession() : new EventLogSession(null))
            {
                Assert.Throws<ArgumentNullException>(() => session.ExportLog(null, PathType.LogName, LogName, GetTestFilePath()));
                Assert.Throws<ArgumentNullException>(() => session.ExportLog(LogName, PathType.LogName, LogName, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => session.ExportLog(LogName, (PathType)0, LogName, GetTestFilePath()));
                Assert.Throws<EventLogNotFoundException>(() => session.ExportLog(LogName, PathType.FilePath, LogName, GetTestFilePath()));
                // Does not throw:
                session.ExportLog(LogName, PathType.LogName, LogName, GetTestFilePath(), tolerateQueryErrors: true);
                session.CancelCurrentOperations();
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void GetProviderNames_WithPassword_Throws()
        {
            var password = new SecureString();
            password.AppendChar('a');
            using (var session = new EventLogSession(null, null, null, password, SessionAuthentication.Default))
            {
                Assert.Throws<UnauthorizedAccessException>(() => session.GetProviderNames());
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void ClearLog_LogNameNullEmptyOrNotExist_Throws()
        {
            using (var session = new EventLogSession())
            {
                Assert.Throws<ArgumentNullException>(() => session.ClearLog(null));
                Assert.Throws<ArgumentNullException>(() => session.ClearLog(null, backupPath: GetTestFilePath()));
                Assert.Throws<EventLogException>(() => session.ClearLog(""));
                Assert.Throws<EventLogNotFoundException>(() => session.ClearLog(logName: nameof(ClearLog_LogNameNullEmptyOrNotExist_Throws)));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void ClearLog_LogExists_Success()
        {
            using (var session = new EventLogSession())
            {
                string log = "Log_" + nameof(ClearLog_LogExists_Success);
                string source = "Source_" + nameof(ClearLog_LogExists_Success);
                try
                {
                    EventLog.CreateEventSource(source, log);
                    using (EventLog eventLog = new EventLog())
                    {
                        eventLog.Source = source;
                        Helpers.Retry(() => eventLog.WriteEntry("Writing to event log."));
                        Assert.NotEqual(0, Helpers.Retry((() => eventLog.Entries.Count)));
                        session.ClearLog(logName: log);
                        Assert.Equal(0,  Helpers.Retry((() => eventLog.Entries.Count)));
                    }
                }
                finally
                {
                    EventLog.DeleteEventSource(source);
                }
                session.CancelCurrentOperations();
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void EventLogExceptionShouldHaveHResultSet()
        {
            using (var session = new EventLogSession())
            {
                EventLogNotFoundException exception = Assert.Throws<EventLogNotFoundException>(() => session.ExportLog(LogName, PathType.FilePath, LogName, GetTestFilePath()));
                Assert.Equal(unchecked((int)0x80070002), exception.HResult);
                session.CancelCurrentOperations();
            }
        }
    }
}
