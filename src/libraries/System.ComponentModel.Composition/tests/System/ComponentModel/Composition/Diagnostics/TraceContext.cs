// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.ComponentModel.Composition.Diagnostics
{
    public partial class TraceContext : IDisposable
    {
        private readonly SourceLevels _previousLevel = TraceSourceTraceWriter.Source.Switch.Level;
        private readonly TraceContextTraceListener _listener = new TraceContextTraceListener();

        public TraceContext(SourceLevels level)
        {
            TraceSourceTraceWriter.Source.Switch.Level = level;
            TraceSourceTraceWriter.Source.Listeners.Add(_listener);
        }
        
        public TraceEventDetails LastTraceEvent
        {
            get { return _listener.TraceEvents.LastOrDefault(); }
        }
        
        public IList<TraceEventDetails> TraceEvents
        {
            get { return _listener.TraceEvents; }
        }

        public void Dispose()
        {
            TraceSourceTraceWriter.Source.Listeners.Remove(_listener);
            TraceSourceTraceWriter.Source.Switch.Level = _previousLevel;            
        }
    }
}
