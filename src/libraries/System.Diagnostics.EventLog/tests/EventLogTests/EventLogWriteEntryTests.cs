// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogWriteEntryTests
    {
        private string message = "EventLogWriteEntryTestsMessage";

        private byte[] myRawData = new byte[4] { 0, 1, 2, 3 };
        private EventInstance myEvent = new EventInstance(0, 1);
        private string[] insertStrings = { "ExtraText", "MoreText" };
        private string[] insertStringsSingleton = { "ExtraText" };

        private EventLogEntry WriteLogEntry(string source, bool type = false, bool instance = false, bool category = false, bool data = false)
        {
            using (EventLog myLog = new EventLog())
            {
                myLog.Source = source;
                if (instance)
                {
                    EventLog.WriteEvent(source, myEvent);
                    if (data)
                    {
                        myLog.WriteEntry(message, EventLogEntryType.Warning, (int)myEvent.InstanceId, (short)myEvent.CategoryId, myRawData);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }
                    else if (category)
                    {

                        myLog.WriteEntry(message, EventLogEntryType.Warning, (int)myEvent.InstanceId, (short)myEvent.CategoryId);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }
                    else
                    {
                        myLog.WriteEntry(message, EventLogEntryType.Warning, (int)myEvent.InstanceId);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }
                }
                else if (type)
                {
                    myLog.WriteEntry(message, EventLogEntryType.Warning);
                }
                else
                {
                    myLog.WriteEntry(message);
                }

                return myLog.Entries[myLog.Entries.Count - 1];
            }
        }

        private EventLogEntry WriteLogEntryWithSource(string source, bool type = false, bool instance = false, bool category = false, bool data = false)
        {
            using (EventLog myLog = new EventLog())
            {
                myLog.Source = source;
                if (instance)
                {

                    EventLog.WriteEvent(source, myEvent);
                    if (data)
                    {
                        EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)myEvent.InstanceId, (short)myEvent.CategoryId, myRawData);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }
                    else if (category)
                    {
                        EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)myEvent.InstanceId, (short)myEvent.CategoryId);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }
                    else
                    {
                        EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)myEvent.InstanceId);
                        return myLog.Entries[myLog.Entries.Count - 1];
                    }


                }
                else if (type)
                {
                    EventLog.WriteEntry(source, message, EventLogEntryType.Warning);
                }
                else
                {
                    EventLog.WriteEntry(source, message);
                }

                return myLog.Entries[myLog.Entries.Count - 1];
            }
        }

        private EventLogEntry WriteLogEntryEventSource(string source, bool data = false)
        {
            if (data)
                EventLog.WriteEvent(source, myEvent, myRawData, insertStrings);
            else
                EventLog.WriteEvent(source, myEvent, insertStrings);

            using (EventLog myLog = new EventLog())
            {
                myLog.Source = source;
                return myLog.Entries[myLog.Entries.Count - 1];
            }

        }

        private EventLogEntry WriteLogEntryEvent(string source, bool data = false)
        {
            using (EventLog myLog = new EventLog())
            {
                myLog.Source = source;
                if (data)
                    myLog.WriteEvent(myEvent, myRawData, insertStringsSingleton);
                else
                    myLog.WriteEvent(myEvent, insertStringsSingleton);

                return myLog.Entries[myLog.Entries.Count - 1];
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntry(bool sourceFlag)
        {
            string log = "Entry";
            string source = "Source" + nameof(WriteEntry);
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source);
                else
                    eventLogEntry = WriteLogEntryWithSource(source);

                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal(source, eventLogEntry.Source);
                Assert.StartsWith(Environment.MachineName, eventLogEntry.MachineName);
                Assert.Equal(eventLogEntry.TimeWritten, eventLogEntry.TimeGenerated);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithType(bool sourceFlag)
        {
            string source = "Source" + nameof(WriteEntryWithType);
            string log = "TypeEntry";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source, true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, true);

                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal(EventLogEntryType.Warning, eventLogEntry.EntryType);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeAndId(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            string source = "Source" + nameof(WriteEntryWithTypeAndId);
            string log = "InstanceEntry";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source, true, true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, true, true);

                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal((int)myEvent.InstanceId, eventLogEntry.InstanceId);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeIdAndCategory(bool sourceFlag)
        {
            string source = "Source" + nameof(WriteEntryWithTypeIdAndCategory);
            string log = "CategoryEntry";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source, true, true, true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, true, true, true);

                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal((short)myEvent.CategoryId, eventLogEntry.CategoryNumber);
                Assert.Equal("(1)", eventLogEntry.Category);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeIdCategoryAndData(bool sourceFlag)
        {
            string source = "Source" + nameof(WriteEntryWithTypeIdCategoryAndData);
            string log = "EntryData";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source, true, true, true, true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, true, true, true, true);

                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal(myRawData, eventLogEntry.Data);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithoutSource()
        {
            using (EventLog myLog = new EventLog())
            {
                Assert.Throws<ArgumentException>(() => myLog.WriteEntry(message));
            }

        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithInvalidType()
        {
            using (EventLog myLog = new EventLog())
            {
                string source = "Source_" + nameof(WriteEntryWithInvalidType);
                myLog.Source = source;
                Assert.Throws<InvalidEnumArgumentException>(() => myLog.WriteEntry(message, (EventLogEntryType)7));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithNullOrEmptySource()
        {
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry(null, message));
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry("", message));
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEvent(bool SourceFlag)
        {
            string source = "Source_" + nameof(WriteEvent);
            string log = "Event";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (SourceFlag)
                    eventLogEntry = WriteLogEntryEventSource(source);
                else
                    eventLogEntry = WriteLogEntryEvent(source);

                Assert.All(insertStrings, message => eventLogEntry.Message.Contains(message));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEventWithData(bool SourceFlag)
        {
            string log = "EventData";
            string source = "Source_" + nameof(WriteEventWithData);
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (SourceFlag)
                    eventLogEntry = WriteLogEntryEventSource(source, true);
                else
                    eventLogEntry = WriteLogEntryEvent(source, true);

                Assert.Equal(myRawData, eventLogEntry.Data);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEventInstanceNull()
        {
            string source = "Source_" + nameof(WriteEventInstanceNull);
            Assert.Throws<ArgumentNullException>(() => EventLog.WriteEvent(source, null, insertStrings));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEventMessageValues_OutOfRange()
        {
            string source = "Source_" + nameof(WriteEventMessageValues_OutOfRange);
            string[] empty = new string[1];
            empty[0] = new string('c', 32767);
            Assert.Throws<ArgumentException>(() => EventLog.WriteEvent(source, myEvent, empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndNotWindowsNano))]
        public void WriteWithoutExistingSource()
        {
            string source = "Source_" + nameof(WriteEvent);
            try
            {
                EventLog.WriteEvent(source, myEvent, myRawData, null);
                Assert.Equal(EventLog.LogNameFromSourceName(source, "."), "Application");
            }
            finally
            {
                EventLog.DeleteEventSource(source);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SourceNameMaxLengthExceeded()
        {
            string source = new string('s', 254);
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry(source, message));
        }
    }
}
