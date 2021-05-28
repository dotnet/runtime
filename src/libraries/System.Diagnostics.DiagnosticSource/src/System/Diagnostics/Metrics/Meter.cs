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
        private static LinkedList<Meter> s_allMeters = new LinkedList<Meter>();
        private LinkedList<Instrument>? _instruments;

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
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Version = version;

            s_allMeters.Add(this);
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
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
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
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" /></param>
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
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
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
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableCounter{t}.Observe" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValues, unit, description);

        /// <summary>
        /// Dispose the Meter which will disable all instruments created by this meter.
        /// </summary>
        public void Dispose()
        {
            s_allMeters.Remove(this, (meter1, meter2) => object.ReferenceEquals(meter1, meter2));

            if (_instruments is not null)
            {
                LinkedListNode<Instrument>? current = _instruments.First;

                while (current is not null)
                {
                    current.Value.NotifyForUnpublishedInstrument();
                    current = current.Next;
                }

                _instruments.Clear();
            }
        }

        // AddInstrument will be called when publishing the instrument (i.e. calling Instrument.Publish()).
        internal void AddInstrument(Instrument instrument)
        {
            if (_instruments is null)
            {
                Interlocked.CompareExchange(ref _instruments, new LinkedList<Instrument>(), null);
            }

            Debug.Assert(_instruments is not null);

            _instruments.AddIfNotExist(instrument, (instrument1, instrument2) => object.ReferenceEquals(instrument1, instrument2));
        }

        // Called from MeterListener.Start
        internal static void NotifyListenerWithAllPublishedInstruments(MeterListener listener)
        {
            Action<Instrument, MeterListener>? instrumentPublished = listener.InstrumentPublished;
            if (instrumentPublished is null)
            {
                return;
            }

            LinkedListNode<Meter>? current = s_allMeters.First;
            while (current is not null)
            {
                LinkedListNode<Instrument>? currentInstrument = current.Value._instruments?.First;
                while (currentInstrument is not null)
                {
                    instrumentPublished.Invoke(currentInstrument.Value, listener);
                    currentInstrument = currentInstrument.Next;
                }
                current = current.Next;
            }
        }
    }
}
