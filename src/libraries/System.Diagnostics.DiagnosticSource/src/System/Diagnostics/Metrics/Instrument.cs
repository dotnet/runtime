// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Base class of all Metrics Instrument classes
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public abstract class Instrument
    {
#if NO_ARRAY_EMPTY_SUPPORT
        internal static KeyValuePair<string, object?>[] EmptyTags { get; } = new KeyValuePair<string, object?>[0];
#else
        internal static KeyValuePair<string, object?>[] EmptyTags => Array.Empty<KeyValuePair<string, object?>>();
#endif // NO_ARRAY_EMPTY_SUPPORT

        // The SyncObject is used to synchronize the following operations:
        //  - Instrument.Publish()
        //  - Meter constructor
        //  - Meter.Dispose
        //  - MeterListener.EnableMeasurementEvents
        //  - MeterListener.DisableMeasurementEvents
        //  - MeterListener.Start
        //  - MeterListener.Dispose
        internal static object SyncObject { get; } = new object();

        // We use LikedList here so we don't have to take any lock while iterating over the list as we always hold on a node which be either valid or null.
        // DiagLinkedList is thread safe for Add and Remove operations.
        internal readonly DiagLinkedList<ListenerSubscription> _subscriptions = new DiagLinkedList<ListenerSubscription>();

        /// <summary>
        /// Protected constructor to initialize the common instrument properties like the meter, name, description, and unit.
        /// All classes extending Instrument need to call this constructor when constructing object of the extended class.
        /// </summary>
        /// <param name="meter">The meter that created the instrument.</param>
        /// <param name="name">The instrument name. cannot be null.</param>
        /// <param name="unit">Optional instrument unit of measurements.</param>
        /// <param name="description">Optional instrument description.</param>
        protected Instrument(Meter meter, string name, string? unit, string? description)
        {
            if (meter == null)
            {
                throw new ArgumentNullException(nameof(meter));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Meter = meter;
            Name = name;
            Description = description;
            Unit = unit;
        }

        /// <summary>
        /// Publish is activating the instrument to start recording measurements and to allow listeners to start listening to such measurements.
        /// </summary>
        protected void Publish()
        {
            List<MeterListener>? allListeners = null;
            lock (Instrument.SyncObject)
            {
                if (Meter.Disposed || !Meter.AddInstrument(this))
                {
                    return;
                }

                allListeners = MeterListener.GetAllListeners();
            }

            if (allListeners is not null)
            {
                foreach (MeterListener listener in allListeners)
                {
                    listener.InstrumentPublished?.Invoke(this, listener);
                }
            }
        }

        /// <summary>
        /// Gets the Meter which created the instrument.
        /// </summary>
        public Meter Meter { get; }

        /// <summary>
        /// Gets the instrument name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the instrument description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the instrument unit of measurements.
        /// </summary>
        public string? Unit { get; }

        /// <summary>
        /// Checks if there is any listeners for this instrument.
        /// </summary>
        public bool Enabled => _subscriptions.First is not null;

        /// <summary>
        /// A property tells if the instrument is an observable instrument.
        /// </summary>
        public virtual bool IsObservable => false;

        // NotifyForUnpublishedInstrument is called from Meter.Dispose()
        internal void NotifyForUnpublishedInstrument()
        {
            DiagNode<ListenerSubscription>? current = _subscriptions.First;
            while (current is not null)
            {
                current.Value.Listener.DisableMeasurementEvents(this);
                current = current.Next;
            }

            _subscriptions.Clear();
        }

        internal static void ValidateTypeParameter<T>()
        {
            Type type = typeof(T);
            if (type != typeof(byte)   && type != typeof(short) && type != typeof(int) && type != typeof(long) &&
                type != typeof(double) && type != typeof(float) && type != typeof(decimal))
            {
                throw new InvalidOperationException(SR.Format(SR.UnsupportedType, type));
            }
        }

        // Called from MeterListener.EnableMeasurementEvents
        internal object? EnableMeasurement(ListenerSubscription subscription, out bool oldStateStored)
        {
            oldStateStored = false;

            if (!_subscriptions.AddIfNotExist(subscription, (s1, s2) => object.ReferenceEquals(s1.Listener, s2.Listener)))
            {
                ListenerSubscription oldSubscription = _subscriptions.Remove(subscription, (s1, s2) => object.ReferenceEquals(s1.Listener, s2.Listener));
                _subscriptions.AddIfNotExist(subscription, (s1, s2) => object.ReferenceEquals(s1.Listener, s2.Listener));
                oldStateStored = object.ReferenceEquals(oldSubscription.Listener, subscription.Listener);
                return oldSubscription.State;
            }

            return false;
        }

        // Called from MeterListener.DisableMeasurementEvents
        internal object? DisableMeasurements(MeterListener listener) => _subscriptions.Remove(new ListenerSubscription(listener), (s1, s2) => object.ReferenceEquals(s1.Listener, s2.Listener)).State;

        internal virtual void Observe(MeterListener listener)
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        internal object? GetSubscriptionState(MeterListener listener)
        {
            DiagNode<ListenerSubscription>? current = _subscriptions.First;
            while (current is not null)
            {
                if (object.ReferenceEquals(listener, current.Value.Listener))
                {
                    return current.Value.State;
                }
                current = current.Next;
            }

            return null;
        }
    }
}
