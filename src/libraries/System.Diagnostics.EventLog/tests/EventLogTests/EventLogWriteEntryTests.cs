// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Diagnostics.Tests
{
    public class EventLogWriteEntryTests
    {
        private const string message = "EventLogWriteEntryTestsMessage";

        private readonly byte[] rawData = new byte[4] { 0, 1, 2, 3 };
        private readonly EventInstance eventInstance = new EventInstance(0, 1);
        private readonly string[] insertStrings = { "ExtraText", "MoreText" };

        private EventLogEntry WriteLogEntry(string source, bool type = false, bool instance = false, bool category = false, bool data = false)
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Source = source;
                if (instance)
                {
                    Helpers.Retry(() => EventLog.WriteEvent(source, eventInstance));
                    if (data)
                    {
                        Helpers.Retry(() => eventLog.WriteEntry(message, EventLogEntryType.Warning, (int)eventInstance.InstanceId, (short)eventInstance.CategoryId, rawData));
                        return eventLog.Entries.LastOrDefault();
                    }
                    else if (category)
                    {
                        Helpers.Retry(() => eventLog.WriteEntry(message, EventLogEntryType.Warning, (int)eventInstance.InstanceId, (short)eventInstance.CategoryId));
                        return eventLog.Entries.LastOrDefault();
                    }
                    else
                    {
                        Helpers.Retry(() => eventLog.WriteEntry(message, EventLogEntryType.Warning, (int)eventInstance.InstanceId));
                        return eventLog.Entries.LastOrDefault();
                    }
                }
                else if (type)
                {
                    Helpers.Retry(() => eventLog.WriteEntry(message, EventLogEntryType.Warning));
                }
                else
                {
                    Helpers.Retry(() => eventLog.WriteEntry(message));
                }

                return eventLog.Entries.LastOrDefault();
            }
        }

        private EventLogEntry WriteLogEntryWithSource(string source, bool type = false, bool instance = false, bool category = false, bool data = false)
        {
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Source = source;
                if (instance)
                {
                    Helpers.Retry(() => EventLog.WriteEvent(source, eventInstance));
                    if (data)
                    {
                        Helpers.Retry(() => EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)eventInstance.InstanceId, (short)eventInstance.CategoryId, rawData));
                        return eventLog.Entries.LastOrDefault();
                    }
                    else if (category)
                    {
                        Helpers.Retry(() => EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)eventInstance.InstanceId, (short)eventInstance.CategoryId));
                        return eventLog.Entries.LastOrDefault();
                    }
                    else
                    {
                        Helpers.Retry(() => EventLog.WriteEntry(source, message, EventLogEntryType.Warning, (int)eventInstance.InstanceId));
                        return eventLog.Entries.LastOrDefault();
                    }
                }
                else if (type)
                {
                    Helpers.Retry(() => EventLog.WriteEntry(source, message, EventLogEntryType.Warning));
                }
                else
                {
                    Helpers.Retry(() => EventLog.WriteEntry(source, message));
                }

                return eventLog.Entries.LastOrDefault();
            }
        }

        private EventLogEntry WriteLogEntryEventSource(string source, bool data = false)
        {
            if (data)
            {
                Helpers.Retry(() => EventLog.WriteEvent(source, eventInstance, rawData, insertStrings));
            }
            else
            {
                Helpers.Retry(() => EventLog.WriteEvent(source, eventInstance, insertStrings));
            }
            using (EventLog eventLog = new EventLog())
            {
                eventLog.Source = source;
                return eventLog.Entries.LastOrDefault();
            }
        }

        private EventLogEntry WriteLogEntryEvent(string source, bool data = false)
        {
            using (EventLog eventLog = new EventLog())
            {
                string[] insertStringsSingleton = { "ExtraText" };
                eventLog.Source = source;
                if (data)
                    Helpers.Retry(() => eventLog.WriteEvent(eventInstance, rawData, insertStringsSingleton));
                else
                    Helpers.Retry(() => eventLog.WriteEvent(eventInstance, insertStringsSingleton));

                return eventLog.Entries.LastOrDefault();
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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

                if (eventLogEntry != null)
                {
                    Assert.Contains(message, eventLogEntry.Message);
                    Assert.Equal(source, eventLogEntry.Source);
                    Assert.StartsWith(Environment.MachineName.ToLowerInvariant(), eventLogEntry.MachineName.ToLowerInvariant());
                    Assert.Equal(eventLogEntry.TimeWritten, eventLogEntry.TimeGenerated);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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
                    eventLogEntry = WriteLogEntry(source, type: true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, type: true);

                if (eventLogEntry != null)
                {
                    Assert.Contains(message, eventLogEntry.Message);
                    Assert.Equal(EventLogEntryType.Warning, eventLogEntry.EntryType);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteEntryWithTypeAndId(bool sourceFlag)
        {
            string source = "Source" + nameof(WriteEntryWithTypeAndId);
            string log = "InstanceEntry";
            try
            {
                EventLog.CreateEventSource(source, log);
                EventLogEntry eventLogEntry;
                if (sourceFlag)
                    eventLogEntry = WriteLogEntry(source, type: true, instance: true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, type: true, instance: true);

                if (eventLogEntry != null)
                {
                    Assert.Contains(message, eventLogEntry.Message);
                    Assert.Equal((int)eventInstance.InstanceId, eventLogEntry.InstanceId);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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
                    eventLogEntry = WriteLogEntry(source, type: true, instance: true, category: true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, type: true, instance: true, category: true);

                // There is some prefix string already attached to the message passed
                // The description for Event ID '0' in Source 'SourceWriteEntryWithTypeIDAndCategory' cannot be found.  The local computer may not have the necessary registry information or message DLL files to display the message, or you may not have permission
                // to access them.  The following information is part of the event:'EventLogWriteEntryTestsMessage'
                // The last part is the associated message
                // The initial message is due in insufficient permission to access resource library EventLogMsgs.dll
                if (eventLogEntry != null)
                {
                    Assert.Contains(message, eventLogEntry.Message);
                    Assert.Equal((short)eventInstance.CategoryId, eventLogEntry.CategoryNumber);
                    Assert.Equal("(" + eventLogEntry.CategoryNumber + ")", eventLogEntry.Category);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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
                    eventLogEntry = WriteLogEntry(source, type: true, instance: true, category: true, data: true);
                else
                    eventLogEntry = WriteLogEntryWithSource(source, type: true, instance: true, category: true, data: true);

                if (eventLogEntry != null)
                {
                    Assert.Contains(message, eventLogEntry.Message);
                    Assert.Equal(rawData, eventLogEntry.Data);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteEntryWithoutSource()
        {
            using (EventLog eventLog = new EventLog())
            {
                Assert.Throws<ArgumentException>(() => eventLog.WriteEntry(message));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteEntryWithInvalidType()
        {
            using (EventLog eventLog = new EventLog())
            {
                string source = "Source_" + nameof(WriteEntryWithInvalidType);
                eventLog.Source = source;
                Assert.Throws<InvalidEnumArgumentException>(() => eventLog.WriteEntry(message, (EventLogEntryType)7)); // 7 is a random number which is not associated with any type in EventLogEntryType
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteEntryWithNullOrEmptySource()
        {
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry(null, message));
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry("", message));
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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

                if (eventLogEntry != null)
                    Assert.All(insertStrings, message => eventLogEntry.Message.Contains(message));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Unreliable Win32 API call
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
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
                    eventLogEntry = WriteLogEntryEventSource(source, data: true);
                else
                    eventLogEntry = WriteLogEntryEvent(source, data: true);

                if (eventLogEntry != null)
                    Assert.Equal(rawData, eventLogEntry.Data);
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetrySilently(() => EventLog.Delete(log));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteEventInstanceNull()
        {
            string source = "Source_" + nameof(WriteEventInstanceNull);
            Helpers.Retry(() => Assert.Throws<ArgumentNullException>(() => EventLog.WriteEvent(source, null, insertStrings)));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteEventMessageValues_OutOfRange()
        {
            string source = "Source_" + nameof(WriteEventMessageValues_OutOfRange);
            string[] message = new string[1];
            message[0] = new string('c', 32767);
            Helpers.Retry(() => Assert.Throws<ArgumentException>(() => EventLog.WriteEvent(source, eventInstance, message)));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteWithoutExistingSource()
        {
            string source = "Source_" + nameof(WriteWithoutExistingSource);
            try
            {
                Helpers.Retry(() => EventLog.WriteEvent(source, eventInstance, rawData, null));
                Assert.Equal("Application", EventLog.LogNameFromSourceName(source, "."));
            }
            finally
            {
                EventLog.DeleteEventSource(source);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void SourceNameMaxLengthExceeded()
        {
            string source = new string('s', 254);
            Assert.Throws<ArgumentException>(() => EventLog.WriteEntry(source, message));
        }
    }

    internal static class EventLogEntryCollectionExtensions
    {
        internal static EventLogEntry LastOrDefault(this EventLogEntryCollection elec)
        {
            return Helpers.Retry(() => elec.Count > 0 ? elec[elec.Count - 1] : null);
        }
    }
}
