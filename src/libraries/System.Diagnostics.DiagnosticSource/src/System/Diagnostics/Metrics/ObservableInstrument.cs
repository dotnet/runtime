// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// ObservableInstrument{T} is the base class from which all metrics observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public abstract class ObservableInstrument<T> : Instrument where T : struct
    {
        /// <summary>
        /// Create the metrics observable instrument using the properties meter, name, description, and unit.
        /// All classes extending ObservableInstrument{T} need to call this constructor when constructing object of the extended class.
        /// </summary>
        /// <param name="meter">The meter that created the instrument.</param>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        protected ObservableInstrument(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, tags: null)
        {
        }

        /// <summary>
        /// Create the metrics observable instrument using the properties meter, name, description, and unit.
        /// All classes extending ObservableInstrument{T} need to call this constructor when constructing object of the extended class.
        /// </summary>
        /// <param name="meter">The meter that created the instrument.</param>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">tags to attach to the counter.</param>
        protected ObservableInstrument(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            ValidateTypeParameter<T>();
        }

        /// <summary>
        /// Observe() fetches the current measurements being tracked by this instrument. All classes extending ObservableInstrument{T} need to implement this method.
        /// </summary>
        protected abstract IEnumerable<Measurement<T>> Observe();

        /// <summary>
        /// A property tells if the instrument is an observable instrument. This property will return true for all metrics observable instruments.
        /// </summary>
        public override bool IsObservable => true;

        // Returns the underlying user callback for built-in observable instruments, or null for user-defined subclasses.
        // Subclasses provide their measurements via the abstract Observe() method instead.
        internal virtual object? Callback => null;

        // Will be called from MeterListener.RecordObservableInstruments for each observable instrument.
        internal override void Observe(MeterListener listener)
        {
            object? state = GetSubscriptionState(listener);

            // Fast path for the built-in observable instruments: dispatch their callbacks
            // directly to the listener, avoiding the per-observation Measurement<T>[1] allocation that
            // the IEnumerable<Measurement<T>> path used to require.
            object? callback = Callback;

            if (callback is Func<T> valueOnlyFunc)
            {
                listener.NotifyMeasurement(this, valueOnlyFunc(), Instrument.EmptyTags, state);
                return;
            }

            if (callback is Func<Measurement<T>> measurementOnlyFunc)
            {
                Measurement<T> measurement = measurementOnlyFunc();
                listener.NotifyMeasurement(this, measurement.Value, measurement.Tags, state);
                return;
            }

            // Func<IEnumerable<Measurement<T>>> built-ins and user-defined ObservableInstrument<T> subclasses
            // both fall through to the virtual Observe() override.
            IEnumerable<Measurement<T>> measurements = Observe();
            if (measurements is null)
            {
                return;
            }

            foreach (Measurement<T> measurement in measurements)
            {
                listener.NotifyMeasurement(this, measurement.Value, measurement.Tags, state);
            }
        }
    }
}
