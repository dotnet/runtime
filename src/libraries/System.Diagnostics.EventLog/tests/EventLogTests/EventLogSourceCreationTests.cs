// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogSourceCreationTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void CheckSourceExistenceAndDeletion()
        {
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
            Assert.False(EventLog.SourceExists(null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void DeleteUnregisteredSource()
        {
            Assert.Throws<System.ArgumentException>(() => EventLog.DeleteEventSource(Guid.NewGuid().ToString("N")));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void LogNameNull()
        {
            string source = "Source_" + nameof(LogNameNull);

            try
            {
                EventLog.CreateEventSource(source, null);
                Assert.True(EventLog.SourceExists(source));
                Assert.Equal(EventLog.LogNameFromSourceName(source, "."), "Application");
            }
            finally
            {
                EventLog.DeleteEventSource(source);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void SourceNameNull()
        {
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(null, null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void IncorrectLogName()
        {
            string source = "Source_" + nameof(IncorrectLogName);
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, "?"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void SourceNameMaxLengthExceeded()
        {
            string source = new string('s', 254);
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void SourceDataNull()
        {
            Assert.Throws<ArgumentNullException>(() => EventLog.CreateEventSource(null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void SourceAlreadyExistWhileCreation()
        {
            string source = "Source_" + nameof(SourceAlreadyExistWhileCreation);
            string log = "MyLog";
            try
            {
                EventLog.CreateEventSource(source, log);
                Assert.True(EventLog.SourceExists(source));
                Assert.Throws<FormatException>(() => EventLog.CreateEventSource(source, log));

            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }

            Assert.False(EventLog.SourceExists(source));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void LogNameExists()
        {
            string source = "Source_" + nameof(LogNameExists);
            string log = "AppEvent";

            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, log));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SourceCreationInvalidData()
        {
            string log = "InvalidData";
            string source = "Source_" + nameof(SourceCreationInvalidData);

            EventSourceCreationData mySourceData = new EventSourceCreationData(source, log);
            Assert.Throws<ArgumentOutOfRangeException>(() => mySourceData.CategoryCount = -1);
        }

    }
}
