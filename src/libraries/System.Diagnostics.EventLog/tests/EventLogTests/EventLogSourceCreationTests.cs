// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogSourceCreationTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckSourceExistenceAndDeletion()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            string source = "Source_" + nameof(EventLogSourceCreationTests);
            string log = "MyLog";
            try
            {
                EventLog.CreateEventSource(source, log);
                Assert.True(EventLog.SourceExists(source));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }

            Assert.False(EventLog.SourceExists(source));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckSourceExistsArgumentNull()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            Assert.False(EventLog.SourceExists(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void DeleteUnregisteredSource()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            Assert.Throws<System.ArgumentException>(() => EventLog.DeleteEventSource(Guid.NewGuid().ToString("N")));
        }
    }
}
