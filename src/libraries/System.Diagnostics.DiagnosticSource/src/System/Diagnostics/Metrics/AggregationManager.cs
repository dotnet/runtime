// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Metrics
{
    [UnsupportedOSPlatform("browser")]
    [SecuritySafeCritical]
    internal sealed class AggregationManager
    {
        public const double MinCollectionTimeSecs = 0.1;
        private static readonly QuantileAggregation s_defaultHistogramConfig = new QuantileAggregation(new double[] { 0.50, 0.95, 0.99 });

        // these fields are modified after construction and accessed on multiple threads, use lock(this) to ensure the data
        // is synchronized
        private readonly List<Predicate<Instrument>> _instrumentConfigFuncs = new();
        private TimeSpan _collectionPeriod;

        private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new();
        private readonly CancellationTokenSource _cts = new();
        private Thread? _collectThread;
        private readonly MeterListener _listener;
        private int _currentTimeSeries;
        private int _currentHistograms;

        private readonly int _maxTimeSeries;
        private readonly int _maxHistograms;
        private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
        private readonly Action<DateTime, DateTime> _beginCollection;
        private readonly Action<DateTime, DateTime> _endCollection;
        private readonly Action<Instrument> _beginInstrumentMeasurements;
        private readonly Action<Instrument> _endInstrumentMeasurements;
        private readonly Action<Instrument> _instrumentPublished;
        private readonly Action _initialInstrumentEnumerationComplete;
        private readonly Action<Exception> _collectionError;
        private readonly Action _timeSeriesLimitReached;
        private readonly Action _histogramLimitReached;
        private readonly Action<Exception> _observableInstrumentCallbackError;

        public AggregationManager(
            int maxTimeSeries,
            int maxHistograms,
            Action<Instrument, LabeledAggregationStatistics> collectMeasurement,
            Action<DateTime, DateTime> beginCollection,
            Action<DateTime, DateTime> endCollection,
            Action<Instrument> beginInstrumentMeasurements,
            Action<Instrument> endInstrumentMeasurements,
            Action<Instrument> instrumentPublished,
            Action initialInstrumentEnumerationComplete,
            Action<Exception> collectionError,
            Action timeSeriesLimitReached,
            Action histogramLimitReached,
            Action<Exception> observableInstrumentCallbackError)
        {
            _maxTimeSeries = maxTimeSeries;
            _maxHistograms = maxHistograms;
            _collectMeasurement = collectMeasurement;
            _beginCollection = beginCollection;
            _endCollection = endCollection;
            _beginInstrumentMeasurements = beginInstrumentMeasurements;
            _endInstrumentMeasurements = endInstrumentMeasurements;
            _instrumentPublished = instrumentPublished;
            _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
            _collectionError = collectionError;
            _timeSeriesLimitReached = timeSeriesLimitReached;
            _histogramLimitReached = histogramLimitReached;
            _observableInstrumentCallbackError = observableInstrumentCallbackError;

            _listener = new MeterListener()
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    _instrumentPublished(instrument);
                    InstrumentState? state = GetInstrumentState(instrument);
                    if (state != null)
                    {
                        _beginInstrumentMeasurements(instrument);
                        listener.EnableMeasurementEvents(instrument, state);
                    }
                },
                MeasurementsCompleted = (instrument, cookie) =>
                {
                    _endInstrumentMeasurements(instrument);
                    RemoveInstrumentState(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<double>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<float>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<long>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<int>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<short>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<byte>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
            _listener.SetMeasurementEventCallback<decimal>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
        }

        public void Include(string meterName)
        {
            Include(i => i.Meter.Name == meterName);
        }

        public void Include(string meterName, string instrumentName)
        {
            Include(i => i.Meter.Name == meterName && i.Name == instrumentName);
        }

        private void Include(Predicate<Instrument> instrumentFilter)
        {
            lock (this)
            {
                _instrumentConfigFuncs.Add(instrumentFilter);
            }
        }

        public AggregationManager SetCollectionPeriod(TimeSpan collectionPeriod)
        {
            // The caller, MetricsEventSource, is responsible for enforcing this
            Debug.Assert(collectionPeriod.TotalSeconds >= MinCollectionTimeSecs);
            lock (this)
            {
                _collectionPeriod = collectionPeriod;
            }
            return this;
        }

        public void Start()
        {
            // if already started or already stopped we can't be started again
            Debug.Assert(_collectThread == null && !_cts.IsCancellationRequested);
            Debug.Assert(_collectionPeriod.TotalSeconds >= MinCollectionTimeSecs);

            // This explicitly uses a Thread and not a Task so that metrics still work
            // even when an app is experiencing thread-pool starvation. Although we
            // can't make in-proc metrics robust to everything, this is a common enough
            // problem in .NET apps that it feels worthwhile to take the precaution.
            _collectThread = new Thread(() => CollectWorker(_cts.Token));
            _collectThread.IsBackground = true;
            _collectThread.Name = "MetricsEventSource CollectWorker";
            _collectThread.Start();

            _listener.Start();
            _initialInstrumentEnumerationComplete();
        }

        private void CollectWorker(CancellationToken cancelToken)
        {
            try
            {
                double collectionIntervalSecs = -1;
                lock (this)
                {
                    collectionIntervalSecs = _collectionPeriod.TotalSeconds;
                }
                Debug.Assert(collectionIntervalSecs >= MinCollectionTimeSecs);

                DateTime startTime = DateTime.UtcNow;
                DateTime intervalStartTime = startTime;
                while (!cancelToken.IsCancellationRequested)
                {
                    // intervals end at startTime + X*collectionIntervalSecs. Under normal
                    // circumstance X increases by 1 each interval, but if the time it
                    // takes to do collection is very large then we might need to skip
                    // ahead multiple intervals to catch back up.
                    //
                    DateTime now = DateTime.UtcNow;
                    double secsSinceStart = (now - startTime).TotalSeconds;
                    double alignUpSecsSinceStart = Math.Ceiling(secsSinceStart / collectionIntervalSecs) *
                        collectionIntervalSecs;
                    DateTime nextIntervalStartTime = startTime.AddSeconds(alignUpSecsSinceStart);

                    // The delay timer precision isn't exact. We might have a situation
                    // where in the previous loop iterations intervalStartTime=20.00,
                    // nextIntervalStartTime=21.00, the timer was supposed to delay for 1s but
                    // it exited early so we looped around and DateTime.Now=20.99.
                    // Aligning up from DateTime.Now would give us 21.00 again so we also need to skip
                    // forward one time interval
                    DateTime minNextInterval = intervalStartTime.AddSeconds(collectionIntervalSecs);
                    if (nextIntervalStartTime <= minNextInterval)
                    {
                        nextIntervalStartTime = minNextInterval;
                    }

                    // pause until the interval is complete
                    TimeSpan delayTime = nextIntervalStartTime - now;
                    if (cancelToken.WaitHandle.WaitOne(delayTime))
                    {
                        // don't do collection if timer may not have run to completion
                        break;
                    }

                    // collect statistics for the completed interval
                    _beginCollection(intervalStartTime, nextIntervalStartTime);
                    Collect();
                    _endCollection(intervalStartTime, nextIntervalStartTime);
                    intervalStartTime = nextIntervalStartTime;
                }
            }
            catch (Exception e)
            {
                _collectionError(e);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            if (_collectThread != null)
            {
                _collectThread.Join();
                _collectThread = null;
            }
            _listener.Dispose();
        }

        private void RemoveInstrumentState(Instrument instrument)
        {
            _instrumentStates.TryRemove(instrument, out _);
        }

        private InstrumentState? GetInstrumentState(Instrument instrument)
        {
            if (!_instrumentStates.TryGetValue(instrument, out InstrumentState? instrumentState))
            {
                lock (this) // protect _instrumentConfigFuncs list
                {
                    foreach (Predicate<Instrument> filter in _instrumentConfigFuncs)
                    {
                        if (filter(instrument))
                        {
                            instrumentState = BuildInstrumentState(instrument);
                            if (instrumentState != null)
                            {
                                _instrumentStates.TryAdd(instrument, instrumentState);
                                // I don't think it is possible for the instrument to be removed immediately
                                // and instrumentState = _instrumentStates[instrument] should work, but writing
                                // this defensively.
                                _instrumentStates.TryGetValue(instrument, out instrumentState);
                            }
                            break;
                        }
                    }
                }
            }
            return instrumentState;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                        Justification = "MakeGenericType is creating instances over reference types that works fine in AOT.")]
        internal InstrumentState? BuildInstrumentState(Instrument instrument)
        {
            Func<Aggregator?>? createAggregatorFunc = GetAggregatorFactory(instrument);
            if (createAggregatorFunc == null)
            {
                return null;
            }
            Type aggregatorType = createAggregatorFunc.GetType().GenericTypeArguments[0];
            Type instrumentStateType = typeof(InstrumentState<>).MakeGenericType(aggregatorType);
            return (InstrumentState)Activator.CreateInstance(instrumentStateType, createAggregatorFunc)!;
        }

        private Func<Aggregator?>? GetAggregatorFactory(Instrument instrument)
        {
            Type type = instrument.GetType();
            Type? genericDefType = null;
            genericDefType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genericDefType == typeof(Counter<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        return CheckTimeSeriesAllowed() ? new CounterAggregator(isMonotonic: true) : null;
                    }
                };
            }
            else if (genericDefType == typeof(ObservableCounter<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        return CheckTimeSeriesAllowed() ? new ObservableCounterAggregator(isMonotonic: true) : null;
                    }
                };
            }
            else if (genericDefType == typeof(ObservableGauge<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        return CheckTimeSeriesAllowed() ? new LastValue() : null;
                    }
                };
            }
            else if (genericDefType == typeof(Histogram<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        // checking currentHistograms first because avoiding unexpected increment of TimeSeries count.
                        return (!CheckHistogramAllowed() || !CheckTimeSeriesAllowed()) ?
                            null :
                            new ExponentialHistogramAggregator(s_defaultHistogramConfig);
                    }
                };
            }
            else if (genericDefType == typeof(UpDownCounter<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        return CheckTimeSeriesAllowed() ? new CounterAggregator(isMonotonic: false) : null;
                    }
                };
            }
            else if (genericDefType == typeof(ObservableUpDownCounter<>))
            {
                return () =>
                {
                    lock (this)
                    {
                        return CheckTimeSeriesAllowed() ? new ObservableCounterAggregator(isMonotonic: false) : null;
                    }
                };
            }
            else
            {
                return null;
            }
        }

        private bool CheckTimeSeriesAllowed()
        {
            if (_currentTimeSeries < _maxTimeSeries)
            {
                _currentTimeSeries++;
                return true;
            }
            else if (_currentTimeSeries == _maxTimeSeries)
            {
                _currentTimeSeries++;
                _timeSeriesLimitReached();
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool CheckHistogramAllowed()
        {
            if (_currentHistograms < _maxHistograms)
            {
                _currentHistograms++;
                return true;
            }
            else if (_currentHistograms == _maxHistograms)
            {
                _currentHistograms++;
                _histogramLimitReached();
                return false;
            }
            else
            {
                return false;
            }
        }

        internal void Collect()
        {
            try
            {
                _listener.RecordObservableInstruments();
            }
            catch (Exception e)
            {
                _observableInstrumentCallbackError(e);
            }

            foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
            {
                kv.Value.Collect(kv.Key, (LabeledAggregationStatistics labeledAggStats) =>
                {
                    _collectMeasurement(kv.Key, labeledAggStats);
                });
            }
        }
    }
}
