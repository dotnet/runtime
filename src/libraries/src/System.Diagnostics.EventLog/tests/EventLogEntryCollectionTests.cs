// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogEntryCollectionTests : IDisposable
    {
        private string source = Guid.NewGuid().ToString("N");
        private string message = Guid.NewGuid().ToString("N");
        private string log = Guid.NewGuid().ToString("N");

        public EventLogEntryCollectionTests()
        {
            SourceCreate();
            Console.WriteLine("Constructor");
        }

        private void SourceCreate()
        {
            EventLog.CreateEventSource(source, log);
            Console.WriteLine("Hello");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CopyingEventLogEntryCollection()
        {
            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();
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
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EntryEqualityNullTest()
        {
            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();
                myLog.Source = source;
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                Assert.False(entry.Equals(null));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EntryEqualityAndIndexTest()
        {
            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();
                myLog.Source = source;
                myLog.Clear();
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                Assert.True(entry.Equals(entry));
                Assert.Equal(entry.Index, 1);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EntryInEqualityTest()
        {
            using (EventLog myLog = new EventLog())
            {
                if (!EventLog.SourceExists(source))
                    SourceCreate();
                myLog.Source = source;
                myLog.WriteEntry(message);
                myLog.WriteEntry(message);
                EventLogEntry entry = myLog.Entries[myLog.Entries.Count - 1];
                EventLogEntry secondEntry = myLog.Entries[myLog.Entries.Count - 2];
                Assert.False(entry.Equals(secondEntry));
            }
        }

        public void Dispose()
        {
            if (EventLog.SourceExists(source))
            {
                EventLog.DeleteEventSource(source);
                EventLog.Delete(log);
            }
            GC.SuppressFinalize(this);
        }

        ~EventLogEntryCollectionTests()
        {
            Dispose();
        }
    }
}