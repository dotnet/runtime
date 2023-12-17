// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// A delegate to represent the MeterListener callbacks used in measurements recording operation.
    /// </summary>
    public delegate void MeasurementCallback<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct;

    /// <summary>
    /// MeterListener is class used to listen to the metrics instrument measurements recording.
    /// </summary>
    public sealed class MeterListener : IDisposable
    {
        // We use LikedList here so we don't have to take any lock while iterating over the list as we always hold on a node which be either valid or null.
        // DiagLinkedList is thread safe for Add, Remove, and Clear operations.
        private static readonly List<MeterListener> s_allStartedListeners = new List<MeterListener>();

        // List of the instruments which the current listener is listening to.
        private readonly DiagLinkedList<Instrument> _enabledMeasurementInstruments = new DiagLinkedList<Instrument>();
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
            if (!Meter.IsSupported)
            {
                return;
            }

            bool oldStateStored = false;
            bool enabled = false;
            object? oldState = null;

            lock (Instrument.SyncObject)
            {
                if (instrument is not null && !_disposed && !instrument.Meter.Disposed)
                {
                    _enabledMeasurementInstruments.AddIfNotExist(instrument, object.ReferenceEquals);
                    oldState = instrument.EnableMeasurement(new ListenerSubscription(this, state), out oldStateStored);
                    enabled = true;
                }
            }

            if (enabled)
            {
                if (oldStateStored && MeasurementsCompleted is not null)
                {
                    MeasurementsCompleted?.Invoke(instrument!, oldState);
                }
            }
            else
            {
                // The caller trying to enable the measurements but it didn't happen because the meter or the listener is disposed.
                // We need to send MeasurementsCompleted notification telling this instrument is not enabled for measuring.
                MeasurementsCompleted?.Invoke(instrument!, state);
            }
        }

        /// <summary>
        /// Stop listening to a specific instrument measurement recording.
        /// </summary>
        /// <param name="instrument">The instrument to stop listening to.</param>
        /// <returns>The state object originally passed to <see cref="EnableMeasurementEvents" /> method.</returns>
        public object? DisableMeasurementEvents(Instrument instrument)
        {
            if (!Meter.IsSupported)
            {
                return default;
            }

            object? state = null;
            lock (Instrument.SyncObject)
            {
                if (instrument is null || _enabledMeasurementInstruments.Remove(instrument, object.ReferenceEquals) == default)
                {
                    return default;
                }

                state = instrument.DisableMeasurements(this);
            }

            MeasurementsCompleted?.Invoke(instrument, state);
            return state;
        }

        /// <summary>
        /// Sets a callback for a specific numeric type to get the measurement recording notification from all instruments which enabled listening and was created with the same specified numeric type.
        /// If a measurement of type T is recorded and a callback of type T is registered, that callback will be used.
        /// </summary>
        /// <param name="measurementCallback">The callback which can be used to get measurement recording of numeric type T.</param>
        public void SetMeasurementEventCallback<T>(MeasurementCallback<T>? measurementCallback) where T : struct
        {
            if (!Meter.IsSupported)
            {
                return;
            }

            measurementCallback ??= (instrument, measurement, tags, state) => { /* no-op */};

            if (typeof(T) == typeof(byte))
            {
                _byteMeasurementCallback = (MeasurementCallback<byte>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(int))
            {
                _intMeasurementCallback = (MeasurementCallback<int>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(float))
            {
                _floatMeasurementCallback = (MeasurementCallback<float>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(double))
            {
                _doubleMeasurementCallback = (MeasurementCallback<double>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(decimal))
            {
                _decimalMeasurementCallback = (MeasurementCallback<decimal>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(short))
            {
                _shortMeasurementCallback = (MeasurementCallback<short>)(object)measurementCallback;
            }
            else if (typeof(T) == typeof(long))
            {
                _longMeasurementCallback = (MeasurementCallback<long>)(object)measurementCallback;
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.UnsupportedType, typeof(T)));
            }
        }

        /// <summary>
        /// Enable the listener to start listening to instruments measurement recording.
        /// </summary>
        public void Start()
        {
            if (!Meter.IsSupported)
            {
                return;
            }

            List<Instrument>? publishedInstruments = null;
            lock (Instrument.SyncObject)
            {
                if (_disposed)
                {
                    return;
                }

                if (!s_allStartedListeners.Contains(this))
                {
                    s_allStartedListeners.Add(this);
                    publishedInstruments = Meter.GetPublishedInstruments();
                }
            }

            if (publishedInstruments is not null)
            {
                foreach (Instrument instrument in publishedInstruments)
                {
                    InstrumentPublished?.Invoke(instrument, this);
                }
            }
        }

        /// <summary>
        /// Calls all Observable instruments which the listener is listening to then calls <see cref="SetMeasurementEventCallback" /> with every collected measurement.
        /// </summary>
        public void RecordObservableInstruments()
        {
            if (!Meter.IsSupported)
            {
                return;
            }

            List<Exception>? exceptionsList = null;
            DiagNode<Instrument>? current = _enabledMeasurementInstruments.First;
            while (current is not null)
            {
                if (current.Value.IsObservable)
                {
                    try
                    {
                        current.Value.Observe(this);
                    }
                    catch (Exception e)
                    {
                        exceptionsList ??= new List<Exception>();
                        exceptionsList.Add(e);
                    }
                }

                current = current.Next;
            }

            if (exceptionsList is not null)
            {
                throw new AggregateException(exceptionsList);
            }
        }

        /// <summary>
        /// Disposes the listeners which will stop it from listening to any instrument.
        /// </summary>
        public void Dispose()
        {
            if (!Meter.IsSupported)
            {
                return;
            }

            Dictionary<Instrument, object?>? callbacksArguments = null;
            Action<Instrument, object?>? measurementsCompleted = MeasurementsCompleted;

            lock (Instrument.SyncObject)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                s_allStartedListeners.Remove(this);

                DiagNode<Instrument>? current = _enabledMeasurementInstruments.First;
                if (current is not null && measurementsCompleted is not null)
                {
                    callbacksArguments = new Dictionary<Instrument, object?>();

                    do
                    {
                        object? state = current.Value.DisableMeasurements(this);
                        callbacksArguments.Add(current.Value, state);
                        current = current.Next;
                    } while (current is not null);

                    _enabledMeasurementInstruments.Clear();
                }
            }

            if (callbacksArguments is not null)
            {
                foreach (KeyValuePair<Instrument, object?> kvp in callbacksArguments)
                {
                    measurementsCompleted?.Invoke(kvp.Key, kvp.Value);
                }
            }
        }

        // Publish is called from Instrument.Publish
        internal static List<MeterListener>? GetAllListeners() => s_allStartedListeners.Count == 0 ? null : new List<MeterListener>(s_allStartedListeners);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void NotifyMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                _byteMeasurementCallback(instrument, (byte)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(short))
            {
                _shortMeasurementCallback(instrument, (short)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(int))
            {
                _intMeasurementCallback(instrument, (int)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(long))
            {
                _longMeasurementCallback(instrument, (long)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(float))
            {
                _floatMeasurementCallback(instrument, (float)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(double))
            {
                _doubleMeasurementCallback(instrument, (double)(object)measurement, tags, state);
            }
            if (typeof(T) == typeof(decimal))
            {
                _decimalMeasurementCallback(instrument, (decimal)(object)measurement, tags, state);
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
