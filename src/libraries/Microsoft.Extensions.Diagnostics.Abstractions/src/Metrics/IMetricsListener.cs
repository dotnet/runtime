// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Represents a type used to listen to metrics emitted from the system.
    /// </summary>
    public interface IMetricsListener
    {
        /// <summary>
        /// The name of the listener. This is used to identify the listener in the rules configuration.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Called once by the runtime to provide a <see cref="IObservableInstrumentsSource"/> used to pull for fresh metrics data.
        /// </summary>
        /// <param name="source">A <see cref="IObservableInstrumentsSource"/> that can be called to request current metrics.</param>
        public void Initialize(IObservableInstrumentsSource source);

        /// <summary>
        /// Called when a new instrument is created and enabled by a matching rule.
        /// </summary>
        /// <param name="instrument">The new <see cref="Instrument"/>.</param>
        /// <param name="userState">Listener state associated with this instrument. This will be returned to <see cref="MeasurementCallback{T}"/>
        /// and <see cref="MeasurementsCompleted(Instrument, object?)"/>.</param>
        /// <returns>Returns true if the listener wants to subscribe to this instrument, otherwise false.</returns>
        public bool InstrumentPublished(Instrument instrument, out object? userState);

        /// <summary>
        /// Called when a instrument is disabled by the producer or a rules change.
        /// </summary>
        /// <param name="instrument">The <see cref="Instrument"/> being disabled.</param>
        /// <param name="userState">The original listener state returned by <see cref="InstrumentPublished(Instrument, out object?)"/>.</param>
        public void MeasurementsCompleted(Instrument instrument, object? userState);

        /// <summary>
        /// Called once to get the <see cref="MeasurementHandlers"/> that will be used to process measurements.
        /// </summary>
        /// <returns>The <see cref="MeasurementHandlers"/>.</returns>
        public MeasurementHandlers GetMeasurementHandlers();
    }
}
