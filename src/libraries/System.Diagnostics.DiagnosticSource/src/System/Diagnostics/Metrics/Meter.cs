// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Meter is the class responsible for creating and tracking the Instruments.
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public class Meter : IDisposable
    {
        private static readonly List<Meter> s_allMeters = new List<Meter>();
        private List<Instrument> _instruments = new List<Instrument>();
        internal bool Disposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Meter using the meter name.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        public Meter(string name) : this (name, null) {}

        /// <summary>
        /// Initializes a new instance of the Meter using the meter name and version.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        /// <param name="version">The optional Meter version.</param>
        public Meter(string name, string? version)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version;

            lock (Instrument.SyncObject)
            {
                s_allMeters.Add(this);
            }

            // Ensure the metrics EventSource has been created in case we need to log this meter
            GC.KeepAlive(MetricsEventSource.Log);
        }

        /// <summary>
        /// Returns the Meter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the Meter Version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Create a metrics Counter object.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Counter is an Instrument which supports non-negative increments.
        /// Example uses for Counter: count the number of bytes received, count the number of requests completed, count the number of accounts created, count the number of checkpoints run, and count the number of HTTP 5xx errors.
        /// </remarks>
        public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct => new Counter<T>(this, name, unit, description);

        /// <summary>
        /// Histogram is an Instrument which can be used to report arbitrary values that are likely to be statistically meaningful. It is intended for statistics such as histograms, summaries, and percentile.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for Histogram: the request duration and the size of the response payload.
        /// </remarks>
        public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct => new Histogram<T>(this, name, unit, description);

        /// <summary>
        /// Create a metrics UpDownCounter object.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// UpDownCounter is an Instrument which supports reporting positive or negative metric values.
        /// Example uses for UpDownCounter: reporting the change in active requests or queue size.
        /// </remarks>
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct => new UpDownCounter<T>(this, name, unit, description);

        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description);


        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" /></param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValues, unit, description);


        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValue, unit, description);


        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" /></param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValues, unit, description);


        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValues, unit, description);

        /// <summary>
        /// Dispose the Meter which will disable all instruments created by this meter.
        /// </summary>
        public void Dispose()
        {
            List<Instrument>? instruments = null;

            lock (Instrument.SyncObject)
            {
                if (Disposed)
                {
                    return;
                }
                Disposed = true;

                s_allMeters.Remove(this);
                instruments = _instruments;
                _instruments = new List<Instrument>();
            }

            if (instruments is not null)
            {
                foreach (Instrument instrument in instruments)
                {
                    instrument.NotifyForUnpublishedInstrument();
                }
            }
        }

        // AddInstrument will be called when publishing the instrument (i.e. calling Instrument.Publish()).
        internal bool AddInstrument(Instrument instrument)
        {
            if (!_instruments.Contains(instrument))
            {
                _instruments.Add(instrument);
                return true;
            }
            return false;
        }

        // Called from MeterListener.Start
        internal static List<Instrument>? GetPublishedInstruments()
        {
            List<Instrument>? instruments = null;

            if (s_allMeters.Count > 0)
            {
                instruments = new List<Instrument>();

                foreach (Meter meter in s_allMeters)
                {
                    foreach (Instrument instrument in meter._instruments)
                    {
                        instruments.Add(instrument);
                    }
                }
            }

            return instruments;
        }
    }
}
