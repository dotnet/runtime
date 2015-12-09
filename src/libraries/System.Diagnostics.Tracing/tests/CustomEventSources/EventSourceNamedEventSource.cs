using System;
using System.Linq;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif // USE_MDT_EVENTSOURCE

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framewwork), we use this Ifdef make each kind 
#if USE_MDT_EVENTSOURCE
namespace MdtEventSources.DontPollute
#else
namespace SdtEventSources.DontPollute
#endif
{
#if USE_MDT_EVENTSOURCE
    public class EventSource : Microsoft.Diagnostics.Tracing.EventSource
#else
    public class EventSource : System.Diagnostics.Tracing.EventSource
#endif
    {
        // Manifest generation previous failed on EventSources named EventSource.
        [Event(1)]
        public void Event1(string message) { this.WriteEvent(1, message); }
    }
}
