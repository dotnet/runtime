// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// <see cref="Instrument{T}"/> is the base class for all non-observable instruments.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    [DebuggerDisplay("Name = {Name}, Meter = {Meter.Name}")]
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        /// <summary>
        /// Gets the <see cref="InstrumentAdvice{T}"/> associated with the instrument.
        /// </summary>
        public InstrumentAdvice<T>? Advice { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="Instrument{T}"/>.
        /// </summary>
        /// <param name="meter">The meter that created the instrument. Cannot be null.</param>
        /// <param name="name">The instrument name. Cannot be null.</param>
        protected Instrument(Meter meter, string name)
            : this(meter, name, unit: null, description: null, tags: null, advice: null)
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="Instrument{T}"/>.
        /// </summary>
        /// <param name="meter">The meter that created the instrument. Cannot be null.</param>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Instrument(Meter meter, string name, string? unit, string? description)
            : this(meter, name, unit, description, tags: null, advice: null)
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="Instrument{T}"/>.
        /// </summary>
        /// <param name="meter">The meter that created the instrument. Cannot be null.</param>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">Optional instrument tags.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Instrument(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags)
            : this(meter, name, unit, description, tags, advice: null)
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="Instrument{T}"/>.
        /// </summary>
        /// <param name="meter">The meter that created the instrument. Cannot be null.</param>
        /// <param name="name">The instrument name. Cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">Optional instrument tags.</param>
        /// <param name="advice">Optional <see cref="InstrumentAdvice{T}"/>.</param>
        protected Instrument(
            Meter meter,
            string name,
            string? unit = default,
            string? description = default,
            IEnumerable<KeyValuePair<string, object?>>? tags = default,
            InstrumentAdvice<T>? advice = default)
            : base(meter, name, unit, description, tags)
        {
            Advice = advice;

            ValidateTypeParameter<T>();
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        protected void RecordMeasurement(T measurement) => RecordMeasurement(measurement, Instrument.EmptyTags.AsSpan());

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tags">A span of key-value pair tags associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            DiagNode<ListenerSubscription>? current = _subscriptions.First;
            while (current is not null)
            {
                current.Value.Listener.NotifyMeasurement(this, measurement, tags, current.Value.State);
                current = current.Next;
            }
        }
    }
}
