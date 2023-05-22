// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Meter is the class responsible for creating and tracking the Instruments.
    /// </summary>
    public class Meter : IDisposable
    {
        private static readonly List<Meter> s_allMeters = new List<Meter>();
        private List<Instrument> _instruments = new List<Instrument>();
        private Dictionary<string, List<Instrument>> _nonObservableInstrumentsCache = new();
        internal bool Disposed { get; private set; }

        /// <summary>
        /// Initialize a new instance of the Meter using the <see cref="MeterOptions" />.
        /// </summary>
        public Meter(MeterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Debug.Assert(options.Name != null);

            Initialize(options.Name, options.Version, options.Tags, options.Scope);

            Debug.Assert(Name is not null);
        }

        /// <summary>
        /// Initializes a new instance of the Meter using the meter name.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        public Meter(string name) : this (name, null, null, null) {}

        /// <summary>
        /// Initializes a new instance of the Meter using the meter name and version.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        /// <param name="version">The optional Meter version.</param>
        public Meter(string name, string? version) : this (name, version, null, null) {}

        /// <summary>
        /// Initializes a new instance of the Meter using the meter name and version.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        /// <param name="version">The optional Meter version.</param>
        /// <param name="tags">The optional Meter tags.</param>
        /// <param name="scope">The optional Meter scope.</param>
        /// <remarks>
        /// You can use the scope object to link several Meters with a particular scope.
        /// For instance, a dependency injection container can choose to associate all Meters that are created within the container with its own scope.
        /// If the scope object is null, it indicates that the Meter is not linked to any particular scope.
        /// </remarks>
        public Meter(string name, string? version, IEnumerable<KeyValuePair<string, object?>>? tags, object? scope = null)
        {
            Initialize(name, version, tags, scope);
            Debug.Assert(Name is not null);
        }

        private void Initialize(string name, string? version, IEnumerable<KeyValuePair<string, object?>>? tags, object? scope = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version;
            if (tags is not null)
            {
                Tags = new List<KeyValuePair<string, object?>>(tags);
            }
            Scope = scope;

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
        public string Name { get; private set; }

        /// <summary>
        /// Returns the Meter Version.
        /// </summary>
        public string? Version { get; private set; }

        /// <summary>
        /// Returns the tags associated with the Meter.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; private set; }

        /// <summary>
        /// Returns the Meter scope object.
        /// </summary>
        public object? Scope { get; private set; }

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
        public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct => CreateCounter<T>(name, unit, description, tags: null);

        /// <summary>
        /// Create a metrics Counter object.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Counter is an Instrument which supports non-negative increments.
        /// Example uses for Counter: count the number of bytes received, count the number of requests completed, count the number of accounts created, count the number of checkpoints run, and count the number of HTTP 5xx errors.
        /// </remarks>
        public Counter<T> CreateCounter<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
                => (Counter<T>)GetOrCreateInstrument<T>(typeof(Counter<T>), name, unit, description, tags, () => new Counter<T>(this, name, unit, description, tags));

        /// <summary>
        /// Histogram is an Instrument which can be used to report arbitrary values that are likely to be statistically meaningful. It is intended for statistics such as histograms, summaries, and percentile.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <remarks>
        /// Example uses for Histogram: the request duration and the size of the response payload.
        /// </remarks>
        public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct => CreateHistogram<T>(name, unit, description, tags: null);

        /// <summary>
        /// Histogram is an Instrument which can be used to report arbitrary values that are likely to be statistically meaningful. It is intended for statistics such as histograms, summaries, and percentile.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for Histogram: the request duration and the size of the response payload.
        /// </remarks>
        public Histogram<T> CreateHistogram<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
                => (Histogram<T>)GetOrCreateInstrument<T>(typeof(Histogram<T>), name, unit, description, tags, () => new Histogram<T>(this, name, unit, description, tags));

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
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct
                => CreateUpDownCounter<T>(name, unit, description, tags: null);

        /// <summary>
        /// Create a metrics UpDownCounter object.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// UpDownCounter is an Instrument which supports reporting positive or negative metric values.
        /// Example uses for UpDownCounter: reporting the change in active requests or queue size.
        /// </remarks>
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
                => (UpDownCounter<T>)GetOrCreateInstrument<T>(typeof(UpDownCounter<T>), name, unit, description, tags, () => new UpDownCounter<T>(this, name, unit, description, tags));

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
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags);


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
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" /></param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags);

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
                                        new ObservableUpDownCounter<T>(this, name, observeValues, unit, description, tags: null);

        /// <summary>
        /// Create an ObservableUpDownCounter object. ObservableUpDownCounter is an Instrument which reports increasing or decreasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableUpDownCounter: the process heap size or the approximate number of items in a lock-free circular buffer.
        /// </remarks>
        public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableUpDownCounter<T>(this, name, observeValues, unit, description, tags);

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
                                        new ObservableCounter<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValue, unit, description, tags);

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
                                        new ObservableCounter<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" /></param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValue, unit, description, tags);


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
                                        new ObservableCounter<T>(this, name, observeValues, unit, description, tags: null);

        /// <summary>
        /// ObservableCounter is an Instrument which reports monotonically increasing value(s) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        /// <remarks>
        /// Example uses for ObservableCounter: The number of page faults for each process.
        /// </remarks>
        public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableCounter<T>(this, name, observeValues, unit, description, tags);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description, tags);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description, tags: null);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValue">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValue, unit, description, tags);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValues, unit, description, tags: null);

        /// <summary>
        /// ObservableGauge is an asynchronous Instrument which reports non-additive value(s) (e.g. the room temperature - it makes no sense to report the temperature value from multiple rooms and sum them up) when the instrument is being observed.
        /// </summary>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="observeValues">The callback to call to get the measurements when the <see cref="ObservableInstrument{t}.Observe()" /> is called by <see cref="MeterListener.RecordObservableInstruments" />.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct =>
                                        new ObservableGauge<T>(this, name, observeValues, unit, description, tags);

        /// <summary>
        /// Dispose the Meter which will disable all instruments created by this meter.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Dispose the Meter which will disable all instruments created by this meter.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

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
                _nonObservableInstrumentsCache = new();
            }

            if (instruments is not null)
            {
                foreach (Instrument instrument in instruments)
                {
                    instrument.NotifyForUnpublishedInstrument();
                }
            }
        }

        private static bool CompareTags(IEnumerable<KeyValuePair<string, object?>>? tags1, IEnumerable<KeyValuePair<string, object?>>? tags2)
        {
            if (tags1 is null)
            {
                return tags2 is null;
            }

            if (tags2 is null)
            {
                return false;
            }

            if (tags1 is ICollection<KeyValuePair<string, object?>> firstCol && tags2 is ICollection<KeyValuePair<string, object?>> secondCol)
            {
                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<KeyValuePair<string, object?>> firstList && secondCol is IList<KeyValuePair<string, object?>> secondList)
                {
                    int count = firstList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        KeyValuePair<string, object?> pair1 = firstList[i];
                        KeyValuePair<string, object?> pair2 = secondList[i];
                        if (pair1.Key != pair2.Key || !object.Equals(pair1.Value, pair2.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<KeyValuePair<string, object?>> e1 = tags1.GetEnumerator())
            using (IEnumerator<KeyValuePair<string, object?>> e2 = tags2.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    KeyValuePair<string, object?> pair1 = e1.Current;
                    if (!e2.MoveNext() || pair1.Key != e2.Current.Key || !object.Equals(pair1.Value, e2.Current.Value))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }

        // AddInstrument will be called when publishing the instrument (i.e. calling Instrument.Publish()).
        private Instrument GetOrCreateInstrument<T>(Type instrumentType, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags, Func<Instrument> instrumentCreator)
        {
            List<Instrument>? instrumentList;

            lock (_nonObservableInstrumentsCache)
            {
                if (!_nonObservableInstrumentsCache.TryGetValue(name, out instrumentList))
                {
                    instrumentList = new List<Instrument>();
                    _nonObservableInstrumentsCache.Add(name, instrumentList);
                }
            }

            Debug.Assert(instrumentList != null);

            lock (instrumentList)
            {
                foreach (Instrument instrument in instrumentList)
                {
                    if (instrument.GetType() == instrumentType && instrument.Unit == unit &&
                        instrument.Description == description && CompareTags(instrument.Tags, tags))
                    {
                        return instrument;
                    }
                }
            }

            Instrument newInstrument = instrumentCreator.Invoke();
            lock (instrumentList)
            {
                instrumentList.Add(newInstrument);
            }

            return newInstrument;
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
