// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class DefaultMeterFactory : IMeterFactory, IMetricsSource
    {
        private readonly Dictionary<string, List<FactoryMeter>> _cachedMeters = new();
        private readonly MeterListener _meterListener;
        private readonly IMetricsListener[] _listeners;
        private readonly IDisposable? _changeTokenRegistration;
        private readonly ConcurrentDictionary<Instrument, ConcurrentDictionary<IMetricsListener, object?>> _connections = new();
        private IList<InstrumentEnableRule> _rules;
        private bool _disposed;

        public DefaultMeterFactory(IEnumerable<IMetricsListener> listeners, IOptionsMonitor<MetricsEnableOptions> options)
        {
            _listeners = listeners.ToArray();
            _changeTokenRegistration = options.OnChange(UpdateRules);
            UpdateRules(options.CurrentValue, name: null);

            _meterListener = new MeterListener()
            {
                InstrumentPublished = InstrumentPublished,
                MeasurementsCompleted = MeasurementsCompleted,
            };
            RegisterCallbacks();
            _meterListener.Start();
        }

        private void RegisterCallbacks()
        {
            _meterListener.SetMeasurementEventCallback<byte>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<short>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<int>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<long>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<float>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<double>(MeasurementCallback);
            _meterListener.SetMeasurementEventCallback<decimal>(MeasurementCallback);
        }

        private void MeasurementCallback<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct
        {
            if (!_connections.TryGetValue(instrument, out ConcurrentDictionary<IMetricsListener, object?>? listeners) || listeners.IsEmpty)
            {
                return;
            }
            foreach (var pair in listeners)
            {
                pair.Key.GetMeasurementHandler<T>().Invoke(instrument, measurement, tags, pair.Value);
            }
        }

        public Meter Create(MeterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Scope is not null && !object.ReferenceEquals(options.Scope, this))
            {
                throw new InvalidOperationException(SR.InvalidScope);
            }

            Debug.Assert(options.Name is not null);

            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMeterFactory));
                }

                if (_cachedMeters.TryGetValue(options.Name, out List<FactoryMeter>? meterList))
                {
                    foreach (Meter meter in meterList)
                    {
                        if (meter.Version == options.Version && DiagnosticsHelper.CompareTags(meter.Tags as List<KeyValuePair<string, object?>>, options.Tags))
                        {
                            return meter;
                        }
                    }
                }
                else
                {
                    meterList = new List<FactoryMeter>();
                    _cachedMeters.Add(options.Name, meterList);
                }

                object? scope = options.Scope;
                options.Scope = this;
                FactoryMeter m = new FactoryMeter(options.Name, options.Version, options.Tags, scope: this);
                options.Scope = scope;

                meterList.Add(m);
                return m;
            }
        }

        [MemberNotNull(nameof(_rules))]
        private void UpdateRules(MetricsEnableOptions options, string? name)
        {
            lock (_connections)
            {
                _rules = options.Rules;

                if (_disposed)
                {
                    return;
                }

                foreach (var pair in _connections)
                {
                    RefreshConnections(pair.Key, pair.Value);
                }
            }
        }

        private void InstrumentPublished(Instrument instrument, MeterListener listener)
        {
            lock (_connections)
            {
                if (_disposed)
                {
                    return;
                }

                if (_connections.ContainsKey(instrument))
                {
                    Debug.Assert(false, "InstrumentPublished called twice for the same instrument");
                    return;
                }

                var listeners = _connections.GetOrAdd(instrument, static _ => new());
                RefreshConnections(instrument, listeners);
            }
        }

        // Called under _connections lock
        private void RefreshConnections(Instrument instrument, ConcurrentDictionary<IMetricsListener, object?> listeners)
        {
            // Find any that match, pair them
            var newListeners = new HashSet<IMetricsListener>();
            var alreadyListening = !listeners.IsEmpty;
            foreach (var rule in _rules)
            {
                if (Matches(rule, instrument))
                {
                    foreach (var listener in _listeners)
                    {
                        if (Matches(rule, instrument, listener))
                        {
                            newListeners.Add(listener);
                        }
                    }
                }
            }

            // Remove any that are no longer needed
            foreach (var pair in listeners)
            {
                if (!newListeners.Contains(pair.Key))
                {
                    listeners.TryRemove(pair.Key, out var _);
                    pair.Key.MeasurementsCompleted(instrument, pair.Value);
                }
            }

            // Add new ones
            foreach (var listener in newListeners)
            {
                if (!listeners.ContainsKey(listener))
                {
                    var state = listener.InstrumentPublished(instrument);
                    listeners.GetOrAdd(listener, state);
                }
            }

            if (!alreadyListening && !listeners.IsEmpty)
            {
                _meterListener.EnableMeasurementEvents(instrument);
            }
            else if (alreadyListening && listeners.IsEmpty)
            {
                _meterListener.DisableMeasurementEvents(instrument);
            }
        }

        private static bool Matches(InstrumentEnableRule rule, Instrument instrument)
        {
            if (!string.IsNullOrEmpty(rule.InstrumentName) &&
                !string.Equals(rule.InstrumentName, instrument.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(rule.MeterName) &&
                !string.Equals(rule.MeterName, instrument.Meter.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // TODO: Scopes?

            return true;
        }

        private static bool Matches(InstrumentEnableRule rule, Instrument instrument, IMetricsListener listener)
        {
            if (rule.Filter != null)
            {
                return rule.Filter(listener.Name, instrument);
            }

            if (string.IsNullOrEmpty(rule.ListenerName))
            {
                return true;
            }

            return string.Equals(rule.ListenerName, listener.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.ListenerName, listener.GetType().FullName, StringComparison.OrdinalIgnoreCase);
        }

        private void MeasurementsCompleted(Instrument instrument, object? state)
        {
            lock (_connections)
            {
                if (_disposed)
                {
                    return;
                }

                if (_connections.TryRemove(instrument, out var listeners))
                {
                    foreach (var pair in listeners)
                    {
                        pair.Key.MeasurementsCompleted(instrument, pair.Value);
                    }
                }
                else
                {
                    Debug.Assert(false, "InstrumentPublished was not called for this instrument");
                }
            }
        }

        public void RecordObservableInstruments()
        {
            foreach (var pair in _connections)
            {
                var instrument = pair.Key;
                if (instrument.IsObservable)
                {
                    // TODO: We can't downcast because we don't know what the T is.
                    // var = instrument as ObservableInstrument;
                }
            }
        }

        public void Dispose()
        {
            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                foreach (List<FactoryMeter> meterList in _cachedMeters.Values)
                {
                    foreach (FactoryMeter meter in meterList)
                    {
                        meter.Release();
                    }
                }

                _cachedMeters.Clear();
            }

            lock (_connections)
            {
                foreach (var instrumentPair in _connections)
                {
                    foreach (var listenerPair in instrumentPair.Value)
                    {
                        listenerPair.Key.MeasurementsCompleted(instrumentPair.Key, listenerPair.Value);
                    }
                }
            }
        }

        internal sealed class FactoryMeter : Meter
        {
            public FactoryMeter(string name, string? version, IEnumerable<KeyValuePair<string, object?>>? tags, object? scope)
                : base(name, version, tags, scope)
            {
            }

            public void Release() => base.Dispose(true); // call the protected Dispose(bool)

            protected override void Dispose(bool disposing)
            {
                // no-op, disallow users from disposing of the meters created from the factory.
            }
        }
    }
}
