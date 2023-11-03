// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// ObservableCounter is a metrics observable Instrument which reports monotonically increasing value(s) when the instrument is being observed.
    /// e.g. CPU time (for different processes, threads, user mode or kernel mode).
    /// Use Meter.CreateObservableCounter methods to create the observable counter object.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public sealed class ObservableCounter<T> : ObservableInstrument<T> where T : struct
    {
        private readonly object _callback;

        internal ObservableCounter(Meter meter, string name, Func<T> observeValue, string? unit, string? description) : this(meter, name, observeValue, unit, description, tags: null)
        {
        }

        internal ObservableCounter(Meter meter, string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            _callback = observeValue ?? throw new ArgumentNullException(nameof(observeValue));
            Publish();
        }

        internal ObservableCounter(Meter meter, string name, Func<Measurement<T>> observeValue, string? unit, string? description) : this(meter, name, observeValue, unit, description, tags: null)
        {
        }

        internal ObservableCounter(Meter meter, string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            _callback = observeValue ?? throw new ArgumentNullException(nameof(observeValue));
            Publish();
        }

        internal ObservableCounter(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description) : this(meter, name, observeValues, unit, description, tags: null)
        {
        }

        internal ObservableCounter(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            _callback = observeValues ?? throw new ArgumentNullException(nameof(observeValues));
            Publish();
        }

        /// <summary>
        /// Observe() fetches the current measurements being tracked by this observable counter.
        /// </summary>
        protected override IEnumerable<Measurement<T>> Observe() => Observe(_callback);
    }
}
