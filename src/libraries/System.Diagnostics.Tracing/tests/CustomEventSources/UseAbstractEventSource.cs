// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public abstract class UtilBaseEventSource : EventSource
    {
        protected UtilBaseEventSource()
            : base()
        { }
        protected UtilBaseEventSource(bool throwOnEventWriteErrors)
            : base(throwOnEventWriteErrors)
        { }

        protected unsafe void WriteEvent(int eventId, int arg1, short arg2, long arg3)
        {
            if (IsEnabled())
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 2;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 8;
                WriteEventCore(eventId, 3, descrs);
            }
        }
    }

    [EventSource(Name = "OptimizedEventSource")]
    public sealed class OptimizedEventSource : UtilBaseEventSource
    {
        public static OptimizedEventSource Log = new OptimizedEventSource();

        public OptimizedEventSource()
            : base(true)
        { }

        [Event(1,
            Channel = EventChannel.Admin,
            Keywords = Keywords.Kwd1, Level = EventLevel.Informational, Message = "WriteIntToAdmin called with argument {0}")]
        public void WriteToAdmin(int n, short sh, long l)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Kwd1
                , EventChannel.Admin
                ))
                WriteEvent(1, n, sh, l);
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
