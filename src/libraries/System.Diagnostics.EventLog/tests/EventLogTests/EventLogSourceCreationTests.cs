// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogSourceCreationTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void CheckSourceExistenceAndDeletion()
        {
            string source = "Source_" + nameof(EventLogSourceCreationTests);
            string log = "SourceExistenceLog";
            try
            {
                EventLog.CreateEventSource(source, log);
                Assert.True(EventLog.SourceExists(source));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }

            Assert.False(EventLog.SourceExists(source));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void CheckSourceExistsArgumentNull()
        {
            Assert.False(EventLog.SourceExists(null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void DeleteUnregisteredSource()
        {
            Assert.Throws<ArgumentException>(() => EventLog.DeleteEventSource(Guid.NewGuid().ToString("N")));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void LogNameNullMeansApplicationLog()
        {
            string source = "Source_" + nameof(LogNameNullMeansApplicationLog);

            try
            {
                EventLog.CreateEventSource(source, null);
                Assert.True(EventLog.SourceExists(source));
                Assert.Equal("Application", EventLog.LogNameFromSourceName(source, "."));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void SourceNameNull()
        {
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(null, "logName"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void IncorrectLogName()
        {
            string source = "Source_" + nameof(IncorrectLogName);
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, "?"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void SourceNameMaxLengthExceeded()
        {
            string source = new string('s', 254);
            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void SourceDataNull()
        {
            Assert.Throws<ArgumentNullException>(() => EventLog.CreateEventSource(null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void SourceAlreadyExistsWhenCreatingSource()
        {
            string source = "Source_" + nameof(SourceAlreadyExistsWhenCreatingSource);
            string log = "ExistingSource";
            try
            {
                EventLog.CreateEventSource(source, log);
                Assert.True(EventLog.SourceExists(source));
                Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, log));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void LogNameAlreadyExists_Throws()
        {
            string source = "Source_" + nameof(LogNameAlreadyExists_Throws);
            string log = "AppEvent";

            Assert.Throws<ArgumentException>(() => EventLog.CreateEventSource(source, log));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void EventSourceCategoryCount_Invalid()
        {
            string log = "InvalidData";
            string source = "Source_" + nameof(EventSourceCategoryCount_Invalid);

            EventSourceCreationData mySourceData = new EventSourceCreationData(source, log);
            Assert.Throws<ArgumentOutOfRangeException>(() => mySourceData.CategoryCount = -1);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void MessageResourceFile_Set()
        {
            string messageFile = "ResourceFile";
            string source = "Source" + nameof(MessageResourceFile_Set);
            string log = "MessageResourceFile";
            EventSourceCreationData sourceData = new EventSourceCreationData(source, log);
            sourceData.MessageResourceFile = messageFile;
            Assert.Equal(messageFile, sourceData.MessageResourceFile);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void CategoryResourceFile_Set()
        {
            string messageFile = "ResourceFile";
            string source = "Source" + nameof(CategoryResourceFile_Set);
            string log = "MessageResourceFile";
            EventSourceCreationData sourceData = new EventSourceCreationData(source, log);
            sourceData.CategoryResourceFile = messageFile;
            Assert.Equal(messageFile, sourceData.CategoryResourceFile);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void ParameterResourceFile_Set()
        {
            string messageFile = "ResourceFile";
            string source = "Source" + nameof(ParameterResourceFile_Set);
            string log = "MessageResourceFile";
            EventSourceCreationData sourceData = new EventSourceCreationData(source, log);
            sourceData.ParameterResourceFile = messageFile;
            Assert.Equal(messageFile, sourceData.ParameterResourceFile);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void CategoryCount_Set()
        {
            string source = "Source" + nameof(CategoryCount_Set);
            string log = "MessageResourceFile";
            EventSourceCreationData sourceData = new EventSourceCreationData(source, log);
            sourceData.CategoryCount = 2;
            Assert.Equal(2, sourceData.CategoryCount);
        }
    }
}
