// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// An interface registered with each IMetricsListener using <see cref="IMetricsListener.Initialize(IObservableInstrumentsSource)"/>. The listener
    /// can call <see cref="RecordObservableInstruments"/> to receive the current set of measurements for enabled observable instruments.
    /// </summary>
    public interface IObservableInstrumentsSource
    {
        /// <summary>
        /// Requests that the current set of metrics for enabled instruments be sent to the listener's <see cref="MeasurementCallback{T}"/>'s.
        /// </summary>
        public void RecordObservableInstruments();
    }
}
