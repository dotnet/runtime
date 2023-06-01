// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Instrument{T} is the base class for all non-observable instruments.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        /// <summary>
        /// Create the metrics instrument using the properties meter, name, description, and unit.
        /// All classes extending Instrument{T} need to call this constructor when constructing object of the extended class.
        /// </summary>
        /// <param name="meter">The meter that created the instrument.</param>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        protected Instrument(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, tags: null)
        {
        }

        /// <summary>
        /// Create the metrics instrument using the properties meter, name, description, and unit.
        /// All classes extending Instrument{T} need to call this constructor when constructing object of the extended class.
        /// </summary>
        /// <param name="meter">The meter that created the instrument.</param>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        /// <param name="tags">Optional instrument tags.</param>
        protected Instrument(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
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
