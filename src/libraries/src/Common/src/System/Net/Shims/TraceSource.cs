// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// IMPORTANT: This is temporary code.

namespace System.Net
{
    internal class TraceSource
    {
        public TraceSource(string name) { }

        public TraceSource(string name, SourceLevels defaultLevel) { }

        public void Close() { }

        public void Flush() { }

        public void TraceEvent(TraceEventType eventType, int id) { }

        public void TraceEvent(TraceEventType eventType, int id, string message) { }

        public void TraceEvent(TraceEventType eventType, int id, string format, params object[] args) { }

        public void TraceData(TraceEventType eventType, int id, object data) { }

        public void TraceData(TraceEventType eventType, int id, params object[] data) { }

        public void TraceInformation(string message) { }

        public void TraceInformation(string format, params object[] args) { }

        public void TraceTransfer(int id, string message, Guid relatedActivityId) { }

        public SourceSwitch Switch { get { return new SourceSwitch(); } }
    }

    internal enum TraceEventType
    {
        Critical = 0x01,
        Error = 0x02,
        Warning = 0x04,
        Information = 0x08,
        Verbose = 0x10,
        Start = 0x0100,
        Stop = 0x0200,
        Suspend = 0x0400,
        Resume = 0x0800,
        Transfer = 0x1000,
    }

    [Flags]
    internal enum SourceLevels
    {
        Off = 0,
        Critical = 0x01,
        Error = 0x03,
        Warning = 0x07,
        Information = 0x0F,
        Verbose = 0x1F,
        ActivityTracing = 0xFF00,
        All = unchecked((int)0xFFFFFFFF),
    }

    internal class SourceSwitch
    {
        public SourceSwitch()
        {
        }

        public bool ShouldTrace(TraceEventType eventType)
        {
            return false;
        }
    }
}
