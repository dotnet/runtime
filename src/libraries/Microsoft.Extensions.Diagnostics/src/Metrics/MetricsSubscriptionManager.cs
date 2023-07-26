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
using static Microsoft.Extensions.Diagnostics.Metrics.DefaultMeterFactory;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal class MetricsSubscriptionManager : IMetricsSubscriptionManager
    {
        private readonly MeterListener _meterListener;
        private readonly IMetricsListener[] _listeners;
        private readonly IDisposable? _changeTokenRegistration;
        private readonly ConcurrentDictionary<Instrument, ConcurrentDictionary<IMetricsListener, object?>> _connections = new();
        private IList<InstrumentEnableRule> _rules;
        private bool _disposed;

        public MetricsSubscriptionManager(IEnumerable<IMetricsListener> listeners, IOptionsMonitor<MetricsEnableOptions> options)
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
                if (RuleMatchesInstrument(rule, instrument))
                {
                    foreach (var listener in _listeners)
                    {
                        if (RuleMatchesListener(rule, instrument, listener))
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

        private bool RuleMatchesInstrument(InstrumentEnableRule rule, Instrument instrument)
        {
            if (!string.IsNullOrEmpty(rule.InstrumentName) &&
                !string.Equals(rule.InstrumentName, instrument.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(rule.MeterName))
            {
                // TODO: StartsWith. E.g. "System.Net.Http" or "System.Net.Http.*" should match "System.Net.Http.SocketsHttpHandler" and "System.Net.Http.HttpClient".
                if (!string.Equals(rule.MeterName, instrument.Meter.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return rule.Scopes.HasFlag(MeterScope.Global) && instrument.Meter.Scope == null
                || rule.Scopes.HasFlag(MeterScope.Local) && instrument.Meter.Scope == this;
        }

        private static bool RuleMatchesListener(InstrumentEnableRule rule, Instrument instrument, IMetricsListener listener)
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
            _disposed = true;
        }

        public void Start() { }
    }
}
