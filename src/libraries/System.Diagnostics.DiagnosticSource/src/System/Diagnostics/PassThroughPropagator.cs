// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal sealed class PassThroughPropagator : DistributedContextPropagator
    {
        internal static DistributedContextPropagator Instance { get; } = new PassThroughPropagator();

        public override IReadOnlyCollection<string> Fields { get; } = LegacyPropagator.Instance.Fields;

        public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
        {
            if (setter is null)
            {
                return;
            }

            GetRootId(out string? parentId, out string? traceState, out bool isW3c, out IEnumerable<KeyValuePair<string, string?>>? baggage);
            if (parentId is null)
            {
                return;
            }

            setter(carrier, isW3c ? TraceParent : RequestId, parentId);

            if (!string.IsNullOrEmpty(traceState))
            {
                setter(carrier, TraceState, traceState);
            }

            if (baggage is not null)
            {
                InjectBaggage(carrier, baggage, setter);
            }
        }

        public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState) => LegacyPropagator.Instance.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter) => LegacyPropagator.Instance.ExtractBaggage(carrier, getter);

        private static void GetRootId(out string? parentId, out string? traceState, out bool isW3c, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            Activity? activity = Activity.Current;

            while (activity?.Parent is Activity parent)
            {
                activity = parent;
            }

            traceState = activity?.TraceStateString;
            parentId = activity?.ParentId ?? activity?.Id;
            isW3c = parentId is not null ? Activity.TryConvertIdToContext(parentId, traceState, out _) : false;
            baggage = activity?.Baggage;
        }
    }
}
