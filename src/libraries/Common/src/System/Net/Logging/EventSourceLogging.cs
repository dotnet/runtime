// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net-Debug")]
    internal class EventSourceLogging : EventSource
    {
        static private EventSourceLogging s_log = new EventSourceLogging();
        private EventSourceLogging() { }

        static public EventSourceLogging Log
        {
            get
            {
                return s_log;
            }
        }

        [Event(1, Keywords = Keywords.Default)]
        public void DebugMessage(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Keywords = Keywords.Debug)]
        public void DebugDumpArray(byte[] bufferSegmentArray)
        {
            WriteEvent(2, bufferSegmentArray);
        }

        [Event(3, Keywords = Keywords.Debug, Level = EventLevel.Warning)]
        public void WarningDumpArray(string message)
        {
            WriteEvent(3, message);
        }

        [Event(4, Keywords = Keywords.FunctionEntryExit, Message = "{0}({1})")]
        public void FunctionStart(string functionName, string parameters = "*none*")
        {
            WriteEvent(4, functionName, parameters);
        }

        [Event(5, Keywords = Keywords.FunctionEntryExit, Message = "{0} returns {1}")]
        public void FunctionStop(string functionName, string result = "")
        {
            WriteEvent(5, functionName, result);
        }

        [Event(6, Keywords = Keywords.Default, Level = EventLevel.Warning)]
        public void WarningMessage(string message)
        {
            WriteEvent(6, message);
        }

        [Event(7, Keywords = Keywords.Default, Level = EventLevel.Critical)]
        public void AssertFailed(string message, string detailMessage)
        {
            WriteEvent(7, message, detailMessage);
        }

        [Event(8, Keywords = Keywords.Default, Level = EventLevel.Critical)]
        public void CriticalMessage(string message, string detailMessage)
        {
            WriteEvent(8, message, detailMessage);
        }

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
            public const EventKeywords FunctionEntryExit = (EventKeywords)0x0004;
        }
    }
}
