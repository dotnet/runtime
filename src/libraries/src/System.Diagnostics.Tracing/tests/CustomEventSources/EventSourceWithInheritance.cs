// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif // USE_MDT_EVENTSOURCE

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framework), we use this Ifdef make each kind 
#if USE_MDT_EVENTSOURCE
namespace MdtEventSources
#else
namespace SdtEventSources
#endif
{
    public interface ISampleLogger
    {
        void EventFromInterfaceWithNoEventAttribute(string msg);
    }

    public class EventSourceWithInheritance : EventSource, ISampleLogger
    {
        public void EventFromInterfaceWithNoEventAttribute(string msg)
        {
            this.WriteEvent(1, msg);
        }
    }
}
