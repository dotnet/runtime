// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Net.Test.Common
{
    [EventSource(Name = "Microsoft-System-Net-TestLogging")]
    internal class EventSourceTestLogging : EventSource
    {
        private static EventSourceTestLogging s_log = new EventSourceTestLogging();
        private EventSourceTestLogging() { }

        public static EventSourceTestLogging Log
        {
            get
            {
                return s_log;
            }
        }

        [Event(1, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void TestMessage(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void TestVerboseMessage(string message)
        {
            WriteEvent(2, message);
        }

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
