// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Data
{
    [EventSource(Name = "System.Data.DataCommonEventSource")]
    internal sealed class DataCommonEventSource : EventSource
    {
        internal static readonly DataCommonEventSource Log = new DataCommonEventSource();
        private const int TraceEventId = 1;

        [Event(TraceEventId, Level = EventLevel.Informational)]
        internal void Trace(string message)
        {
            WriteEvent(TraceEventId, message);
        }

        [NonEvent]
        internal void Trace<T0>(string format, T0 arg0)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0));
        }
    }
}
