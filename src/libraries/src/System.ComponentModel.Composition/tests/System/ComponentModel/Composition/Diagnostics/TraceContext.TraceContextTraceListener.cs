// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.ComponentModel.Composition.Diagnostics
{
    partial class TraceContext : IDisposable
    {
        private class TraceContextTraceListener : TraceListener
        {
            private readonly Collection<TraceEventDetails> _traceEvents = new Collection<TraceEventDetails>();

            public IList<TraceEventDetails> TraceEvents
            {
                get { return _traceEvents; }
            }

            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
            {
                _traceEvents.Add(new TraceEventDetails(eventCache, source, eventType, (TraceId)id, format, args));
            }

            public override void Write(string message)
            {
                throw new NotImplementedException();
            }

            public override void WriteLine(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
