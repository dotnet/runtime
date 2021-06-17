// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Metrics
{
    internal class AggregationManager
    {
        // these fields are modified after construction and accessed on multiple threads, use lock(this) to ensure the data
        // is synchronized
        private List<Predicate<Instrument>> _instrumentConfigFuncs = new();
        private TimeSpan _collectionPeriod;

        private ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<Instrument, InstrumentState>();
        private CancellationTokenSource _cts = new();
        private Task? _collectTask;
        private MeterListener _listener;

        private Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
        private Action<DateTime> _beginCollection;
        private Action<DateTime> _endCollection;
        private Action<Instrument> _beginInstrumentMeasurements;
        private Action<Instrument> _endInstrumentMeasurements;
        private Action<Instrument> _instrumentPublished;
        private Action _initialInstrumentEnumerationComplete;

        public AggregationManager(
            Action<Instrument, LabeledAggregationStatistics> collectMeasurement,
            Action<DateTime> beginCollection,
            Action<DateTime> endCollection,
            Action<Instrument> beginInstrumentMeasurements,
            Action<Instrument> endInstrumentMeasurements,
            Action<Instrument> instrumentPublished,
            Action initialInstrumentEnumerationComplete)
        {
            _collectMeasurement = collectMeasurement;
            _beginCollection = beginCollection;
            _endCollection = endCollection;
            _beginInstrumentMeasurements = beginInstrumentMeasurements;
            _endInstrumentMeasurements = endInstrumentMeasurements;
            _instrumentPublished = instrumentPublished;
            _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;

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
            if (_collectTask != null || _cts.IsCancellationRequested)
            {
                // correct usage from internal code should never get here
                Debug.Assert(false);
                throw new InvalidOperationException("Start can only be called once");
            }

            if (_collectionPeriod.TotalSeconds < 1)
            {
                throw new InvalidOperationException("CollectionPeriod must be >= 1 sec");
            }

            var token = _cts.Token;
            _collectTask = Task.Run(async () =>
            {
                double collectionIntervalSecs = -1;
                lock (this)
                {
                    collectionIntervalSecs = _collectionPeriod.TotalSeconds;
                }
                if (collectionIntervalSecs < 1)
                {
                    Debug.Fail("_collectionPeriod must be >= 1 sec");
                    return;
                }

                DateTime startTime = DateTime.UtcNow;
                DateTime intervalStartTime = startTime;
                while (!token.IsCancellationRequested)
                {
                    // intervals end at startTime + X*collectionIntervalSecs. Under normal
                    // circumstance X increases by 1 each interval, but if the time it
                    // takes to do collection is very large then we might need to skip
                    // ahead multiple intervals to catch back up. Find the next interval
                    // start time that is still in the future.
                    DateTime now = DateTime.UtcNow;
                    double secsSinceStart = (now - startTime).TotalSeconds;
                    double alignUpSecsSinceStart = Math.Ceiling(secsSinceStart / collectionIntervalSecs) *
                        collectionIntervalSecs;
                    DateTime nextIntervalStartTime = startTime.AddSeconds(alignUpSecsSinceStart);

                    // pause until the interval is complete
                    try
                    {
                        TimeSpan delayTime = nextIntervalStartTime - now;
                        await Task.Delay(delayTime, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    // collect statistics for the completed interval
                    _beginCollection(intervalStartTime);
                    Collect();
                    _endCollection(intervalStartTime);
                    intervalStartTime = nextIntervalStartTime;
                }
            });

            _listener.Start();
            _initialInstrumentEnumerationComplete();
        }

        public void Dispose()
        {
            _cts.Cancel();
            if (_collectTask != null)
            {
                _collectTask.Wait();
                _collectTask = null;
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
                return () => new ExponentialHistogramAggregator(new PercentileAggregation(new double[] { 50, 95, 99 }));
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
