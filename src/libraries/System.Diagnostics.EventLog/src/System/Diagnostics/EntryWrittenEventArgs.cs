// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public class EntryWrittenEventArgs : EventArgs
    {
        public EntryWrittenEventArgs()
        {
        }

        public EntryWrittenEventArgs(EventLogEntry entry)
        {
            Entry = entry;
        }

        public EventLogEntry Entry { get; }
    }
}
