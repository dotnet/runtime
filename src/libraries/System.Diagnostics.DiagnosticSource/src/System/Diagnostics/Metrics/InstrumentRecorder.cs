// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// A helper class to record the measurements published from <see cref="Instrument{T}"/> or <see cref="ObservableInstrument{T}"/>.
    /// </summary>
    public sealed class InstrumentRecorder<T> : IDisposable where T : struct
    {
        private bool _isObservableInstrument;
        private bool _disposed;
        private Instrument? _instrument;

        private MeterListener _meterListener;
        private List<Measurement<T>> _measurements;

        /// <summary>
        /// Initialize a new instance <see cref="InstrumentRecorder{T}" /> to record the measurements published by <see cref="Instrument" />.
        /// </summary>
        /// <param name="instrument">The <see cref="Instrument" /> to record measurements from.</param>
        public InstrumentRecorder(Instrument instrument)
        {
            if (instrument is null)
            {
                throw new ArgumentNullException(nameof(instrument));
            }

            if (instrument is not Instrument<T> and not ObservableInstrument<T>)
            {
                throw new InvalidOperationException(SR.InvalidInstrumentType);
            }

            _measurements = new List<Measurement<T>>();

            _meterListener = new MeterListener();

            _instrument = instrument;
            _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
            _meterListener.EnableMeasurementEvents(instrument, state: null);
            _meterListener.Start();
        }

        /// <summary>
        /// Initialize a new instance <see cref="InstrumentRecorder{T}" /> to record the measurement published by an <see cref="Instrument" /> with the name <paramref name="instrumentName" />
        /// and a meter having the name <paramref name="meterName"/> and scope <paramref name="scopeFilter"/>
        /// </summary>
        /// <param name="scopeFilter">The name of the meter.</param>
        /// <param name="meterName">The name of the meter.</param>
        /// <param name="instrumentName">The name of the instrument.</param>
        public InstrumentRecorder(object? scopeFilter, string meterName, string instrumentName)
        {
            if (meterName is null)
            {
                throw new ArgumentNullException(nameof(meterName));
            }

            if (instrumentName is null)
            {
                throw new ArgumentNullException(nameof(instrumentName));
            }

            Initialize((instrument) => object.Equals(instrument.Meter.Scope, scopeFilter) &&
                                       instrument.Meter.Name == meterName &&
                                       instrument.Name == instrumentName);

            Debug.Assert(_meterListener is not null);
            Debug.Assert(_measurements is not null);
        }

        /// <summary>
        /// Initialize a new instance <see cref="InstrumentRecorder{T}" /> to record the measurement published by an <see cref="Instrument" /> with the name <paramref name="instrumentName" />
        /// and the meter <paramref name="meter"/>.
        /// </summary>
        /// <param name="meter">The meter that produced the instrument required for recording the published measurements.</param>
        /// <param name="instrumentName">The name of the instrument.</param>
        public InstrumentRecorder(Meter meter, string instrumentName)
        {
            if (meter is null)
            {
                throw new ArgumentNullException(nameof(meter));
            }

            if (instrumentName is null)
            {
                throw new ArgumentNullException(nameof(instrumentName));
            }

            Initialize((instrument) => object.ReferenceEquals(instrument.Meter, meter) && instrument.Name == instrumentName);

            Debug.Assert(_meterListener is not null);
            Debug.Assert(_measurements is not null);
        }

        private void Initialize(Func<Instrument, bool> instrumentPredicate)
        {
            _measurements = new List<Measurement<T>>();

            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrumentPredicate(instrument) && (instrument is ObservableInstrument<T> || instrument is Instrument<T>))
                {
                    if (Interlocked.CompareExchange(ref _instrument, instrument, null) is null)
                    {
                        _isObservableInstrument = instrument is ObservableInstrument<T>;
                        listener.EnableMeasurementEvents(instrument, state: null);
                    }
                }
            };

            _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
            _meterListener.Start();
        }

        /// <summary>
        /// Gets the <see cref="Instrument"/> that is being recorded.
        /// </summary>
        public Instrument? Instrument
        {
            get => _instrument;
            private set => _instrument = value;
        }

        private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var m = new Measurement<T>(measurement, tags);

            lock (_measurements)
            {
                _measurements.Add(m);
            }
        }

        /// <summary>
        /// Gets the measurements recorded by this <see cref="InstrumentRecorder{T}"/>.
        /// </summary>
        /// <param name="clear">If true, the previously recorded measurements will be cleared.</param>
        /// <returns>The measurements recorded by this <see cref="InstrumentRecorder{T}"/>.</returns>
        public IEnumerable<Measurement<T>> GetMeasurements(bool clear = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InstrumentRecorder<T>));
            }

            if (_isObservableInstrument)
            {
                _meterListener.RecordObservableInstruments();
            }

            lock (_measurements)
            {
                IEnumerable<Measurement<T>> measurements = _measurements.ToArray();
                if (clear)
                {
                    _measurements.Clear();
                }
                return measurements;
            }
        }

        /// <summary>
        /// Disposes the <see cref="InstrumentRecorder{T}"/> and stops recording measurements.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _meterListener.Dispose();
        }
    }
}
