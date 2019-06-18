// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using Microsoft.DotNet.XUnitExtensions;
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
            using (var session = new EventLogSession())
            {
                EventLogConfiguration configuration;
                try
                {
                    configuration = new EventLogConfiguration(logName, session);
                }
                catch (EventLogNotFoundException)
                {
                    throw new SkipTestException(nameof(EventLogNotFoundException));
                }

                using (configuration)
                {
                    EventLogInformation logInfo = session.GetLogInformation(configuration.LogName, PathType.LogName);

                    Assert.Equal(logInfo.CreationTime, logInfo.CreationTime);
                    Assert.Equal(logInfo.LastAccessTime, logInfo.LastAccessTime);
                    Assert.Equal(logInfo.LastWriteTime, logInfo.LastWriteTime);
                    Assert.Equal(logInfo.FileSize, logInfo.FileSize);
                    Assert.Equal(logInfo.Attributes, logInfo.Attributes);
                    Assert.Equal(logInfo.RecordCount, logInfo.RecordCount);
                    Assert.Equal(logInfo.OldestRecordNumber, logInfo.OldestRecordNumber);
                    Assert.Equal(logInfo.IsLogFull, logInfo.IsLogFull);
                }
            }
        }
    }
}
