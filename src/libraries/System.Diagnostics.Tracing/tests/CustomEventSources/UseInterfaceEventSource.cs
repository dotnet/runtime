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
    public interface IMyLogging
    {
        void Error(int errorCode, string msg);
        void Warning(string msg);
    }

    public sealed class MyLoggingEventSource : EventSource, IMyLogging
    {
        public static MyLoggingEventSource Log = new MyLoggingEventSource();

        [Event(1)]
        public void Error(int errorCode, string msg)
        { WriteEvent(1, errorCode, msg); }

        [Event(2)]
        public void Warning(string msg)
        { WriteEvent(2, msg); }
    }
}
