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
