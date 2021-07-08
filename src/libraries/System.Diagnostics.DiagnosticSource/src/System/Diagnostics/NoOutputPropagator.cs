// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal class NoOutputPropagator : TextMapPropagator
    {
        internal static TextMapPropagator Instance { get; } = new NoOutputPropagator();

        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
        {
            // nothing to do.
        }

        public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string? traceId, out string? traceState) => LegacyPropagator.Instance.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object carrier, PropagatorGetterCallback getter) => LegacyPropagator.Instance.ExtractBaggage(carrier, getter);
    }
}
