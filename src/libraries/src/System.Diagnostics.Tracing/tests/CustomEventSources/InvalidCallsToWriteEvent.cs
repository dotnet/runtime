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
    public sealed class InvalidCallsToWriteEventEventSource : EventSource
    {
        public void WriteTooFewArgs(int m, int n)
        {
            WriteEvent(1, m);
        }

        public void WriteTooManyArgs(string msg)
        {
            WriteEvent(2, msg, "-");
        }
    }
}
