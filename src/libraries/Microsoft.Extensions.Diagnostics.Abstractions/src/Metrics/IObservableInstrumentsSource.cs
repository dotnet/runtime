// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// A callback registered with <see cref="IMetricsListener.Initialize(IObservableInstrumentsSource)"/> that the listener
    /// can call to request the current set of metrics for enabled instruments be sent to the listener's <see cref="MeasurementCallback{T}"/>'s.
    /// </summary>
    public interface IObservableInstrumentsSource
    {
        /// <summary>
        /// Requests that the current set of metrics for enabled instruments be sent to the listener's <see cref="MeasurementCallback{T}"/>'s.
        /// </summary>
        public void RecordObservableInstruments();
    }
}
