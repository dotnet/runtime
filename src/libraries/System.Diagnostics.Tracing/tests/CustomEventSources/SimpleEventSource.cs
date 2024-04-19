// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framework), we use this Ifdef make each kind

namespace SdtEventSources
{
    // Test for when an EventSource is named "EventSource" but in a separate namespace
    // so we don't have to fully qualify everything else using it.
    namespace DontPollute
    {
        public sealed class EventSource :
#if USE_MDT_EVENTSOURCE
            Microsoft.Diagnostics.Tracing.EventSource
#else
            System.Diagnostics.Tracing.EventSource
#endif
        {
            [Event(1)]
            public void EventWrite(int i) { this.WriteEvent(1, i); }
        }
    }

    [EventSource(Name = "SimpleEventSource")]
    public sealed class SimpleEventSource : EventSource
    {
        public SimpleEventSource()
            : base(true)
        { }

        [Event(1,
            Channel = EventChannel.Admin,
            Keywords = Keywords.Kwd1, Level = EventLevel.Informational, Message = "WriteIntToAdmin called with argument {0}")]
        public void WriteIntToAdmin(int n)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Kwd1
                , EventChannel.Admin
                ))
                WriteEvent(1, n);
        }

        [Event(2,
            Channel = EventChannel.Operational,
            Keywords = Keywords.Kwd1, Level = EventLevel.Informational, Message = "WriteStringToOperational called with argument {0}")]
        public void WriteStringToOperational(string msg)
        {
            WriteEvent(2, msg);
        }

        [Event(3)]
        public void WriteSimpleInt(int n)
        {
            WriteEvent(3, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
        /// <summary>
        /// The keyword definitions for the ETW manifest.
        /// </summary>
        public static class Keywords
        {
            public const EventKeywords Kwd1 = (EventKeywords)1;
            public const EventKeywords Kwd2 = (EventKeywords)2;
        }

        /// <summary>
        /// The task definitions for the ETW manifest.
        /// </summary>
        public static class Tasks
        {
            public const EventTask Http = (EventTask)1;
        }

        public static class Opcodes
        {
            public const EventOpcode Delete = (EventOpcode)100;
        }
        #endregion
    }
}
