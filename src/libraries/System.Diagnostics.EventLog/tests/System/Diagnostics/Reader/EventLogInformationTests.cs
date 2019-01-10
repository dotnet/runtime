// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogInformationTests
    {
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void GetLogInformation_NullLogName_Throws(bool usingDefaultCtor)
        {
            using (var session = usingDefaultCtor ? new EventLogSession() : new EventLogSession(null))
            {
                Assert.Throws<ArgumentNullException>(() => session.GetLogInformation(null, PathType.LogName));
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData("Microsoft-Windows-TaskScheduler/Operational")]
        [InlineData("Application")]
        public void GetLogInformation_UsingLogName_DoesNotThrow(string logName)
        {
            DateTime? creationTime, lastAccessTime, lastWriteTime;
            long? fileSize, recordCount, oldestRecordNumber;
            int? attributes;
            bool? isLogFull;
            using (var session = new EventLogSession())
            {
                EventLogConfiguration configuration = null;
                try
                {
                    configuration = new EventLogConfiguration(logName, session);
                }
                catch (EventLogNotFoundException)
                {
                    configuration?.Dispose();
                    return;
                }

                EventLogInformation logInfo = session.GetLogInformation(configuration.LogName, PathType.LogName);
                creationTime = logInfo.CreationTime;
                lastAccessTime = logInfo.LastAccessTime;
                lastWriteTime = logInfo.LastWriteTime;
                fileSize = logInfo.FileSize;
                attributes = logInfo.Attributes;
                recordCount = logInfo.RecordCount;
                oldestRecordNumber = logInfo.OldestRecordNumber;
                isLogFull = logInfo.IsLogFull;

                configuration.Dispose();
            }
            using (var session = new EventLogSession())
            {
                using (var configuration = new EventLogConfiguration(logName, session))
                {
                    EventLogInformation logInfo = session.GetLogInformation(configuration.LogName, PathType.LogName);
                    Assert.Equal(creationTime, logInfo.CreationTime);
                    Assert.Equal(lastAccessTime, logInfo.LastAccessTime);
                    Assert.Equal(lastWriteTime, logInfo.LastWriteTime);
                    Assert.Equal(fileSize, logInfo.FileSize);
                    Assert.Equal(attributes, logInfo.Attributes);
                    Assert.Equal(recordCount, logInfo.RecordCount);
                    Assert.Equal(oldestRecordNumber, logInfo.OldestRecordNumber);
                    Assert.Equal(isLogFull, logInfo.IsLogFull);
                }
            }
        }
    }
}