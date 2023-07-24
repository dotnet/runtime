// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public interface IMetricsListener
    {
        public string Name { get; }
        public void SetSource(IMetricsSource source);
        public object? InstrumentPublished(Instrument instrument);
        public void MeasurementsCompleted(Instrument instrument, object? userState);
        public MeasurementCallback<T> GetMeasurementHandler<T>() where T : struct;
    }
}
