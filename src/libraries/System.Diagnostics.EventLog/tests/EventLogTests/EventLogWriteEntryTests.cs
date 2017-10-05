// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogWriteEntryTests
    {

        private string source = Guid.NewGuid().ToString("N");
        private string message = "EventLogWriteEntryTestsMessage";
        private string log = Guid.NewGuid().ToString("N");

        private byte[] myRawData = new byte[4] { 0, 1, 2, 3 };
        private EventInstance myEvent = new EventInstance(0, 1);
        private string[] insertStrings = { "ExtraText", "MoreText" };
        private string[] insertStringsSingleton = { "ExtraText" };

        public EventLogWriteEntryTests()
        {
            SourceCreate();
        }

        private void SourceCreate()
        {
            EventLog.CreateEventSource(source, log);
        }

        private EventLogEntry GetLogEntry(bool type = false, bool instance = false, bool cat = false, bool data = false)
        {
            if (!EventLog.SourceExists(source))
                SourceCreate();
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
                    else
                    {
                        if (cat)
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

        private EventLogEntry GetLogEntrywithSource(bool type = false, bool instance = false, bool cat = false, bool data = false)
        {
            if (!EventLog.SourceExists(source))
                SourceCreate();
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
                    else
                    {
                        if (cat)
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

        private EventLogEntry GetLogEntryEventSource(bool data = false)
        {
            if (data)
                EventLog.WriteEvent(source, myEvent, myRawData, insertStrings);
            else
                EventLog.WriteEvent(source, myEvent, insertStrings);

            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();

                myLog.Source = source;
                return myLog.Entries[myLog.Entries.Count - 1];
            }

        }

        private EventLogEntry GetLogEntryEvent(bool data = false)
        {
            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();

                myLog.Source = source;
                if (data)
                    myLog.WriteEvent(myEvent, myRawData, insertStringsSingleton);
                else
                    myLog.WriteEvent(myEvent, insertStringsSingleton);

                return myLog.Entries[myLog.Entries.Count - 1];
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntry(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (sourceFlag)
                eventLogEntry = GetLogEntry();
            else
                eventLogEntry = GetLogEntrywithSource();

            Assert.Contains(message, eventLogEntry.Message);
            Assert.Equal(source, eventLogEntry.Source);
            Assert.StartsWith(Environment.MachineName, eventLogEntry.MachineName);

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithType(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (sourceFlag)
                eventLogEntry = GetLogEntry(true);
            else
                eventLogEntry = GetLogEntrywithSource(true);

            Assert.Contains(message, eventLogEntry.Message);
            Assert.Equal(EventLogEntryType.Warning, eventLogEntry.EntryType);

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeAndId(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (sourceFlag)
                eventLogEntry = GetLogEntry(true, true);
            else
                eventLogEntry = GetLogEntrywithSource(true, true);

            Assert.Contains(message, eventLogEntry.Message);
            Assert.Equal((int)myEvent.InstanceId, eventLogEntry.InstanceId);

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeIdAndCategory(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (sourceFlag)
                eventLogEntry = GetLogEntry(true, true, true);
            else
                eventLogEntry = GetLogEntrywithSource(true, true, true);

            Assert.Contains(message, eventLogEntry.Message);
            // check on category number
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeIdCategoryAndBinData(bool sourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (sourceFlag)
                eventLogEntry = GetLogEntry(true, true, true, true);
            else
                eventLogEntry = GetLogEntrywithSource(true, true, true, true);

            Assert.Contains(message, eventLogEntry.Message);
            Assert.Equal(myRawData, eventLogEntry.Data);

        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithoutSource()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            using (EventLog myLog = new EventLog())
            {
                Assert.Throws<ArgumentException>(() => myLog.WriteEntry(message));
            }

        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithInvalidType()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();

                myLog.Source = source;
                Assert.Throws<InvalidEnumArgumentException>(() => myLog.WriteEntry(message, (EventLogEntryType)7));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEntryWithNullOrEmptySource()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry(null, message));
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry("", message));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEvent(bool SourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (SourceFlag)
                eventLogEntry = GetLogEntryEventSource();
            else
                eventLogEntry = GetLogEntryEvent();

            Assert.All(insertStrings, message => eventLogEntry.Message.Contains(message));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEventWithData(bool SourceFlag)
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            EventLogEntry eventLogEntry;
            if (SourceFlag)
                eventLogEntry = GetLogEntryEventSource(true);
            else
                eventLogEntry = GetLogEntryEvent(true);

            Assert.Equal(myRawData, eventLogEntry.Data);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEventInstanceNull()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            Assert.Throws<ArgumentNullException>(() => EventLog.WriteEvent(source, null, insertStrings));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void WriteEventMessageValues()
        {
            if (!AdminHelpers.IsProcessElevated())
                return;

            string[] empty = new string[1];
            empty[0] = new string('c', 32767);
            Assert.Throws<ArgumentException>(() => EventLog.WriteEvent(source, myEvent, empty));
        }

        ~EventLogWriteEntryTests()
        {
            EventLog.DeleteEventSource(source);
            EventLog.Delete(log);
        }
    }
}

