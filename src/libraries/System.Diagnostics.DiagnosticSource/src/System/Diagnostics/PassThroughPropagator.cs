// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal class PassThroughPropagator : TextMapPropagator
    {
        internal static TextMapPropagator Instance { get; } = new PassThroughPropagator();

        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
        {
            GetRootId(out string? parentId, out string? traceState, out bool isW3c, out IEnumerable<KeyValuePair<string, string?>>? baggage);
            if (parentId is null)
            {
                return;
            }

            setter(carrier, isW3c ? TraceParent : RequestId, parentId);

            if (traceState is not null)
            {
                setter(carrier, TraceState, traceState);
            }

            if (baggage is not null)
            {
                InjectBaggage(carrier, baggage, setter);
            }
        }

        public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string? traceId, out string? traceState) => LegacyPropagator.Instance.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object carrier, PropagatorGetterCallback getter) => LegacyPropagator.Instance.ExtractBaggage(carrier, getter);

        private static void GetRootId(out string? parentId, out string? traceState, out bool isW3c, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            Activity? activity = Activity.Current;
            if (activity is null)
            {
                parentId = null;
                traceState = null;
                isW3c = false;
                baggage = null;
                return;
            }

            while (activity is not null && activity.Parent is not null)
            {
                activity = activity.Parent;
            }

            traceState = activity?.TraceStateString;
            parentId = activity?.ParentId ?? activity?.Id;
            isW3c = activity?.IdFormat == ActivityIdFormat.W3C;
            baggage = activity?.Baggage;
        }
    }
}
