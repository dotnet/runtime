// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if FEATURE_TRACING

using System;
using System.Diagnostics;
using Microsoft.Internal;

namespace Microsoft.Composition.Diagnostics
{
    // Represents a trace writer that writes to a System.Diagnostics TraceSource
    internal sealed class TraceSourceTraceWriter : TraceWriter
    {
        internal static readonly TraceSource Source = new TraceSource("Microsoft.Composition", SourceLevels.Warning);

        public override bool CanWriteInformation
        {
            get { return Source.Switch.ShouldTrace(TraceEventType.Information); }
        }

        public override bool CanWriteWarning
        {
            get { return Source.Switch.ShouldTrace(TraceEventType.Warning); }
        }

        public override bool CanWriteError
        {
            get { return Source.Switch.ShouldTrace(TraceEventType.Error); }
        }

        public override void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Information, traceId, format, arguments);
        }

        public override void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Warning, traceId, format, arguments);
        }

        public override void WriteError(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Error, traceId, format, arguments);
        }

        private static void WriteEvent(TraceEventType eventType, CompositionTraceId traceId, string format, params object[] arguments)
        {
            Source.TraceEvent(eventType, (int)traceId, format, arguments);
        }
    }
}

#endif //FEATURE_TRACING
