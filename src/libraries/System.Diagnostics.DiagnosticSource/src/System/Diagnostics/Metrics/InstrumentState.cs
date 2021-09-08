// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security;

namespace System.Diagnostics.Metrics
{
    internal abstract class InstrumentState
    {
        // This can be called concurrently with Collect()
        [SecuritySafeCritical]
        public abstract void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels);

        // This can be called concurrently with Update()
        public abstract void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc);
    }


    internal sealed class InstrumentState<TAggregator> : InstrumentState
        where TAggregator : Aggregator
    {
        private AggregatorStore<TAggregator> _aggregatorStore;

        public InstrumentState(Func<TAggregator?> createAggregatorFunc)
        {
            _aggregatorStore = new AggregatorStore<TAggregator>(createAggregatorFunc);
        }

        public override void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc)
        {
            _aggregatorStore.Collect(aggregationVisitFunc);
        }

        [SecuritySafeCritical]
        public override void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels)
        {
            TAggregator? aggregator = _aggregatorStore.GetAggregator(labels);
            aggregator?.Update(measurement);
        }
    }
}
