// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogEntryCollectionTests
    {
        private string message = "EntryCollectionMessage";

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CopyingEventLogEntryCollection()
        {
            string log = "CopyCollection";
            string source = "Source_" + nameof(CopyingEventLogEntryCollection);

            using (EventLog myLog = new EventLog())
            {
                EventLog.CreateEventSource(source, log);
                myLog.Source = source;
                myLog.WriteEntry(message);
                myLog.WriteEntry("Furthur Testing");
                EventLogEntryCollection entryCollection = myLog.Entries;
                EventLogEntry[] entryCollectionCopied = new EventLogEntry[entryCollection.Count];
                entryCollection.CopyTo(entryCollectionCopied, 0);

                int i = 0;
                foreach (EventLogEntry entry in entryCollection)
                {
                    Assert.Equal(entry.Message, entryCollectionCopied[i].Message);
                    i += 1;
                }
            }
            EventLog.DeleteEventSource(source);
            EventLog.Delete(log);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckingEntryEqualityWithNull()
        {
            string log = "NullTest";
            string source = "Source_" + nameof(CheckingEntryEqualityWithNull);

            using (EventLog myLog = new EventLog())
            {
                EventLog.CreateEventSource(source, log);
                myLog.Source = source;
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                Assert.False(entry.Equals(null));
            }
            EventLog.DeleteEventSource(source);
            EventLog.Delete(log);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckingEntryEqualityAndIndex()
        {
            string log = "IndexTest";
            string source = "Source_" + nameof(CheckingEntryEqualityAndIndex);

            using (EventLog myLog = new EventLog())
            {
                EventLog.CreateEventSource(source, log);
                myLog.Source = source;
                myLog.Clear();
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                Assert.True(entry.Equals(entry));
                myLog.WriteEntry(message);
                EventLogEntry secondEntry = myLog.Entries[myLog.Entries.Count - 1];
                Assert.Equal(entry.Index + 1, secondEntry.Index);
            }
            EventLog.DeleteEventSource(source);
            EventLog.Delete(log);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CheckingEntryInEquality()
        {
            string log = "InEqualityTest";
            string source = "Source_" + nameof(CheckingEntryInEquality);

            using (EventLog myLog = new EventLog())
            {
                EventLog.CreateEventSource(source, log);
                myLog.Source = source;
                myLog.WriteEntry(message);
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                EventLogEntry secondEntry = myLog.Entries[myLog.Entries.Count - 2];
                Assert.False(entry.Equals(secondEntry));
            }
            EventLog.DeleteEventSource(source);
            EventLog.Delete(log);
        }
    }
}
