// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// ObservableGauge is an observable Instrument that reports non-additive value(s) when the instrument is being observed.
    /// e.g. the current room temperature
    /// Use Meter.CreateObservableGauge methods to create the observable counter object.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public sealed class ObservableGauge<T> : ObservableInstrument<T> where T : struct
    {
        private readonly object _callback;

        internal ObservableGauge(Meter meter, string name, Func<T> observeValue, string? unit, string? description) : this(meter, name, observeValue, unit, description, tags: null)
        {
        }

        internal ObservableGauge(Meter meter, string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            _callback = observeValue ?? throw new ArgumentNullException(nameof(observeValue));
            Publish();
        }

        internal ObservableGauge(Meter meter, string name, Func<Measurement<T>> observeValue, string? unit, string? description) : this(meter, name, observeValue, unit, description, tags: null)
        {
        }

        internal ObservableGauge(Meter meter, string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            _callback = observeValue ?? throw new ArgumentNullException(nameof(observeValue));
            Publish();
        }

        internal ObservableGauge(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description) : this(meter, name, observeValues, unit, description, tags: null)
        {
        }

        internal ObservableGauge(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
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
