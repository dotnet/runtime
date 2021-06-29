// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Metrics
{
#if OS_SUPPORT_ATTRTIBUTES
    [UnsupportedOSPlatform("browser")]
#endif
    [SecuritySafeCritical]
    internal class AggregationManager
    {
        public const double MinCollectionTime = 0.1;
        private static readonly QuantileAggregation DefaultHistogramConfig = new QuantileAggregation(new double[] { 0.50, 0.95, 0.99 });

        // these fields are modified after construction and accessed on multiple threads, use lock(this) to ensure the data
        // is synchronized
        private List<Predicate<Instrument>> _instrumentConfigFuncs = new();
        private TimeSpan _collectionPeriod;



        private ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new();
        private CancellationTokenSource _cts = new();
        private Thread? _collectThread;
        private MeterListener _listener;

        private Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
        private Action<DateTime> _beginCollection;
        private Action<DateTime> _endCollection;
        private Action<Instrument> _beginInstrumentMeasurements;
        private Action<Instrument> _endInstrumentMeasurements;
        private Action<Instrument> _instrumentPublished;
        private Action _initialInstrumentEnumerationComplete;
        private Action<Exception> _collectionError;

        public AggregationManager(
            Action<Instrument, LabeledAggregationStatistics> collectMeasurement,
            Action<DateTime> beginCollection,
            Action<DateTime> endCollection,
            Action<Instrument> beginInstrumentMeasurements,
            Action<Instrument> endInstrumentMeasurements,
            Action<Instrument> instrumentPublished,
            Action initialInstrumentEnumerationComplete,
            Action<Exception> collectionError)
        {
            _collectMeasurement = collectMeasurement;
            _beginCollection = beginCollection;
            _endCollection = endCollection;
            _beginInstrumentMeasurements = beginInstrumentMeasurements;
            _endInstrumentMeasurements = endInstrumentMeasurements;
            _instrumentPublished = instrumentPublished;
            _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
            _collectionError = collectionError;

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
                    RemoveInstrumentState(instrument, (InstrumentState)cookie!);
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



        public AggregationManager Include(string meterName)
        {
            Include(i => i.Meter.Name == meterName);
            return this;
        }

        public AggregationManager Include(string meterName, string instrumentName)
        {
            Include(i => i.Meter.Name == meterName && i.Name == instrumentName);
            return this;
        }

        public AggregationManager Include(Predicate<Instrument> instrumentFilter)
        {
            lock (this)
            {
                _instrumentConfigFuncs.Add(instrumentFilter);
            }
            return this;
        }

        public AggregationManager SetCollectionPeriod(TimeSpan collectionPeriod)
        {
            lock (this)
            {
                _collectionPeriod = collectionPeriod;
            }
            return this;
        }

        public void Start()
        {
            // if already started or already stopped we can't be started again
            if (_collectThread != null || _cts.IsCancellationRequested)
            {
                // correct usage from internal code should never get here
                throw new InvalidOperationException("Start can only be called once");
            }

            if (_collectionPeriod.TotalSeconds < MinCollectionTime)
            {
                // correct usage from internal code should never get here
                throw new InvalidOperationException($"CollectionPeriod must be >= {MinCollectionTime} sec");
            }

            // This explicitly uses a Thread and not a Task so that metrics still work
            // even when an app is experiencing thread-pool starvation. Although we
            // can't make in-proc metrics robust to everything, this is a common enough
            // problem in .NET apps that it feels worthwhile to take the precaution.
            _collectThread = new Thread(() => CollectWorker(_cts.Token));
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
                if (collectionIntervalSecs < MinCollectionTime)
                {
                    // correct usage from internal code should never get here
                    throw new InvalidOperationException($"_collectionPeriod must be >= {MinCollectionTime} sec");
                }

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
                    cancelToken.WaitHandle.WaitOne(delayTime);

                    // don't do collection if timer may not have run to completion
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // collect statistics for the completed interval
                    _beginCollection(intervalStartTime);
                    Collect();
                    _endCollection(intervalStartTime);
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

        private void RemoveInstrumentState(Instrument instrument, InstrumentState state)
        {
            _instrumentStates.TryRemove(instrument, out InstrumentState? _state);
        }

        private InstrumentState? GetInstrumentState(Instrument instrument)
        {
            if (!_instrumentStates.TryGetValue(instrument, out InstrumentState? instrumentState))
            {
                lock (this) // protect _isntrumentConfigFuncs list
                {
                    foreach (var filter in _instrumentConfigFuncs)
                    {
                        if (filter(instrument))
                        {
                            instrumentState = BuildInstrumentState(instrument);
                            break;
                        }
                    }
                }
                if (instrumentState != null)
                {
                    _instrumentStates.TryAdd(instrument, instrumentState);
                    instrumentState = _instrumentStates[instrument];
                }
            }
            return instrumentState;
        }

        internal InstrumentState? BuildInstrumentState(Instrument instrument)
        {
            Func<Aggregator>? createAggregatorFunc = GetAggregatorFactory(instrument);
            if (createAggregatorFunc == null)
            {
                return null;
            }
            Type aggregatorType = createAggregatorFunc.GetType().GenericTypeArguments[0];
            Type instrumentStateType = typeof(InstrumentState<>).MakeGenericType(aggregatorType);
            return (InstrumentState)Activator.CreateInstance(instrumentStateType, createAggregatorFunc)!;
        }

        private static Func<Aggregator>? GetAggregatorFactory(Instrument instrument)
        {
            Type type = instrument.GetType();
            Type? genericDefType = null;
#if IS_GENERIC_TYPE_SUPPORT
            genericDefType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
#else
            try
            {
                 genericDefType = type.GetGenericTypeDefinition();
            }
            catch {}
#endif
            if (genericDefType == typeof(Counter<>))
            {
                return () => new RateSumAggregator();
            }
            else if (genericDefType == typeof(ObservableCounter<>))
            {
                return () => new RateAggregator();
            }
            else if (genericDefType == typeof(ObservableGauge<>))
            {
                return () => new LastValue();
            }
            else if (genericDefType == typeof(Histogram<>))
            {
                return () => new ExponentialHistogramAggregator(DefaultHistogramConfig);
            }
            else
            {
                return null;
            }
        }

        internal void Collect()
        {
            _listener.RecordObservableInstruments();

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
