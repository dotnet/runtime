// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// ObservableInstrument{T} is the base class from which all metrics observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
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
        protected ObservableInstrument(Meter meter, string name, string? unit, string? description) : base(meter, name, unit, description)
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

        // Will be called from MeterListener.RecordObservableInstruments for each observable instrument.
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        internal override void Observe(MeterListener listener)
        {
            object? state = GetSubscriptionState(listener);

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

        // Will be called from the concrete classes which extends ObservabilityInstrument<T> when calling Observe() method.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IEnumerable<Measurement<T>> Observe(object callback)
        {
            if (callback is Func<T> valueOnlyFunc)
            {
                return new Measurement<T>[1] { new Measurement<T>(valueOnlyFunc()) };
            }

            if (callback is Func<Measurement<T>> measurementOnlyFunc)
            {
                return new Measurement<T>[1] { measurementOnlyFunc() };
            }

            if (callback is Func<IEnumerable<Measurement<T>>> listOfMeasurementsFunc)
            {
                return listOfMeasurementsFunc();
            }

            Debug.Assert(false, "Execution shouldn't reach this point");
            return null;
        }

    }
}
