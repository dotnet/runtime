// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// A delegate to represent the Meterlistener callbacks used in measurements recording operation.
    /// </summary>
    public delegate void MeasurementCallback<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct;

    /// <summary>
    /// MeterListener is class used to listen to the metrics instrument measurements recording.
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public sealed class MeterListener : IDisposable
    {
        // We use LikedList here so we don't have to take any lock while iterating over the list as we always hold on a node which be either valid or null.
        // LinkedList is thread safe for Add, Remove, and Clear operations.
        private static LinkedList<MeterListener> s_allStartedListeners = new LinkedList<MeterListener>();

        // List of the instruments which the current listener is listening to.
        private LinkedList<Instrument> _enabledMeasurementInstruments = new LinkedList<Instrument>();
        private bool _disposed;

        // We initialize all measurement callback with no-op operations so we'll avoid the null checks during the execution;
        private MeasurementCallback<byte>    _byteMeasurementCallback    = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<short>   _shortMeasurementCallback   = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<int>     _intMeasurementCallback     = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<long>    _longMeasurementCallback    = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<float>   _floatMeasurementCallback   = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<double>  _doubleMeasurementCallback  = (instrument, measurement, tags, state) => { /* no-op */ };
        private MeasurementCallback<decimal> _decimalMeasurementCallback = (instrument, measurement, tags, state) => { /* no-op */ };

        /// <summary>
        /// Creates a MeterListener object.
        /// </summary>
        public MeterListener() { }

        /// <summary>
        /// Callbacks to get notification when an instrument is published.
        /// </summary>
        public Action<Instrument, MeterListener>? InstrumentPublished { get; set; }

        /// <summary>
        /// Callbacks to get notification when stopping the measurement on some instrument.
        /// This can happen when the Meter or the Listener is disposed or calling <see cref="Dispose" /> on the listener.
        /// </summary>
        public Action<Instrument, object?>? MeasurementsCompleted { get; set; }

        /// <summary>
        /// Start listening to a specific instrument measurement recording.
        /// </summary>
        /// <param name="instrument">The instrument to listen to.</param>
        /// <param name="state">A state object which will be passed back to the callback getting measurements events.</param>
        public void EnableMeasurementEvents(Instrument instrument, object? state = null)
        {
            if (instrument is null || _disposed)
            {
                return;
            }

            _enabledMeasurementInstruments.AddIfNotExist(instrument, (instrument1, instrument2) => object.ReferenceEquals(instrument1, instrument2));
            instrument.EnableMeasurement(new ListenerSubscription(this, state));
        }

        /// <summary>
        /// Stop listening to a specific instrument measurement recording.
        /// </summary>
        /// <param name="instrument">The instrument to stop listening to.</param>
        /// <returns>The state object originally passed to <see cref="EnableMeasurementEvents" /> method.</returns>
        public object? DisableMeasurementEvents(Instrument instrument)
        {
            if (instrument is null)
            {
                return default;
            }

            _enabledMeasurementInstruments.Remove(instrument, (instrument1, instrument2) => object.ReferenceEquals(instrument1, instrument2));
            object? state =  instrument.DisableMeasurements(this);
            MeasurementsCompleted?.Invoke(instrument!, state);
            return state;
        }

        /// <summary>
        /// Sets a callback for a specific numeric type to get the measurement recording notification from all instruments which enabled listening and was created with the same specified numeric type.
        /// If a measurement of type T is recorded and a callback of type T is registered, that callback will be used.
        /// </summary>
        /// <param name="measurementCallback">The callback which can be used to get measurement recording of numeric type T.</param>
        public void SetMeasurementEventCallback<T>(MeasurementCallback<T>? measurementCallback) where T : struct
        {
            if (measurementCallback is MeasurementCallback<byte> byteCallback)
            {
                _byteMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : byteCallback;
            }
            else if (measurementCallback is MeasurementCallback<int> intCallback)
            {
                _intMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : intCallback;
            }
            else if (measurementCallback is MeasurementCallback<float> floatCallback)
            {
                _floatMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : floatCallback;
            }
            else if (measurementCallback is MeasurementCallback<double> doubleCallback)
            {
                _doubleMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : doubleCallback;
            }
            else if (measurementCallback is MeasurementCallback<decimal> decimalCallback)
            {
                _decimalMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : decimalCallback;
            }
            else if (measurementCallback is MeasurementCallback<short> shortCallback)
            {
                _shortMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : shortCallback;
            }
            else if (measurementCallback is MeasurementCallback<long> longCallback)
            {
                _longMeasurementCallback = (measurementCallback is null) ? ((instrument, measurement, tags, state) => { /* no-op */}) : longCallback;
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.UnsupportedType, typeof(T)));
            }
        }

        /// <summary>
        /// Enable the listener to start listeneing to instruments measurement recording.
        /// </summary>
        public void Start()
        {
            if (_disposed)
            {
                return;
            }

            if (s_allStartedListeners.AddIfNotExist(this, (listener1, listener2) => object.ReferenceEquals(listener1, listener2)))
            {
                Meter.NotifyListenerWithAllPublishedInstruments(this);
            }
        }

        /// <summary>
        /// Calls all Observable instruments which the listener is listening to then calls <see cref="SetMeasurementEventCallback" /> with every collected measurement.
        /// </summary>
        public void RecordObservableInstruments()
        {
            LinkedListNode<Instrument>? current = _enabledMeasurementInstruments.First;
            while (current is not null)
            {
                if (current.Value.IsObservable)
                {
                    current.Value.Observe(this);
                }

                current = current.Next;
            }
        }

        /// <summary>
        /// Disposes the listeners which will stop it from listeneing to any instrument.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            s_allStartedListeners.Remove(this, (listener1, listener2) => object.ReferenceEquals(listener1, listener2));

            LinkedListNode<Instrument>? current = _enabledMeasurementInstruments.First;

            while (current is not null)
            {
                object? state = current.Value.DisableMeasurements(this);
                MeasurementsCompleted?.Invoke(current.Value, state);
                current = current.Next;
            }

            _enabledMeasurementInstruments.Clear();
        }

        // NotifyForPublishedInstrument will be called every time publishing instrument
        internal static void NotifyForPublishedInstrument(Instrument instrument)
        {
            LinkedListNode<MeterListener>? current = s_allStartedListeners.First;
            while (current is not null)
            {
                current.Value.InstrumentPublished?.Invoke(instrument, current.Value);
                current = current.Next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void NotifyMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            if (measurement is byte byteMeasurement)
            {
                _byteMeasurementCallback(instrument, byteMeasurement, tags, state);
            }
            else if (measurement is short shortMeasurement)
            {
                _shortMeasurementCallback(instrument, shortMeasurement, tags, state);
            }
            else if (measurement is int intMeasurement)
            {
                _intMeasurementCallback(instrument, intMeasurement, tags, state);
            }
            else if (measurement is long longMeasurement)
            {
                _longMeasurementCallback(instrument, longMeasurement, tags, state);
            }
            else if (measurement is float floatMeasurement)
            {
                _floatMeasurementCallback(instrument, floatMeasurement, tags, state);
            }
            else if (measurement is double doubleMeasurement)
            {
                _doubleMeasurementCallback(instrument, doubleMeasurement, tags, state);
            }
            else if (measurement is decimal decimalMeasurement)
            {
                _decimalMeasurementCallback(instrument, decimalMeasurement, tags, state);
            }
        }
    }

    internal readonly struct ListenerSubscription
    {
        internal ListenerSubscription(MeterListener listener, object? state = null)
        {
            Listener = listener;
            State = state;
        }

        internal MeterListener Listener { get; }
        internal object? State { get; }
    }
}
