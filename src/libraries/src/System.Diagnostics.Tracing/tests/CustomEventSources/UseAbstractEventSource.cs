using System;

#if false
using System.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif // USE_MDT_EVENTSOURCE

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framewwork), we use this Ifdef make each kind 
#if false
namespace MdtEventSources
#else
namespace SdtEventSources
#endif
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
#if false
            Channel = EventChannel.Admin, 
#endif
            Keywords = Keywords.Kwd1, Level = EventLevel.Informational, Message = "WriteIntToAdmin called with argument {0}")]
        public void WriteToAdmin(int n, short sh, long l)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Kwd1
#if false
                , EventChannel.Admin
#endif
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
