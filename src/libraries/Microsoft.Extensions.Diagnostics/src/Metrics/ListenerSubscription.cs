// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class ListenerSubscription : IObservableInstrumentsSource, IDisposable
    {
        private readonly MeterListener _meterListener;
        private readonly IMetricsListener _metricsListener;
        private readonly IMeterFactory _meterFactory;
        private readonly Dictionary<Instrument, object?> _instruments = new();
        private IList<InstrumentRule> _rules = Array.Empty<InstrumentRule>();
        private bool _disposed;

        internal ListenerSubscription(IMetricsListener metricsListener, IMeterFactory meterFactory)
        {
            _metricsListener = metricsListener;
            _meterFactory = meterFactory;
            _meterListener = new MeterListener();
        }

        public void Initialize()
        {
            _meterListener.InstrumentPublished = InstrumentPublished;
            _meterListener.MeasurementsCompleted = MeasurementsCompleted;
            var handlers = _metricsListener.GetMeasurementHandlers();
            _meterListener.SetMeasurementEventCallback(handlers.ByteHandler);
            _meterListener.SetMeasurementEventCallback(handlers.ShortHandler);
            _meterListener.SetMeasurementEventCallback(handlers.IntHandler);
            _meterListener.SetMeasurementEventCallback(handlers.LongHandler);
            _meterListener.SetMeasurementEventCallback(handlers.FloatHandler);
            _meterListener.SetMeasurementEventCallback(handlers.DoubleHandler);
            _meterListener.SetMeasurementEventCallback(handlers.DecimalHandler);
            _metricsListener.Initialize(this);
            _meterListener.Start();
        }

        private void InstrumentPublished(Instrument instrument, MeterListener _)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                if (_instruments.ContainsKey(instrument))
                {
                    Debug.Assert(false, "InstrumentPublished called for an instrument we're already listening to.");
                    return;
                }

                RefreshInstrument(instrument);
            }
        }

        // Called when we call DisableMeasurementEvents, like when a rule is disabled,
        // or when the meter/factory is disposed.
        private void MeasurementsCompleted(Instrument instrument, object? state)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                if (_instruments.TryGetValue(instrument, out var listenerState))
                {
                    _instruments.Remove(instrument);
                    _metricsListener.MeasurementsCompleted(instrument, listenerState);
                    _meterListener.DisableMeasurementEvents(instrument);
                }
            }
        }

        internal void UpdateRules(IList<InstrumentRule> rules)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                _rules = rules;

                // Get a fresh list of instruments to compare against the new rules.
                using var tempListener = new MeterListener();
                tempListener.InstrumentPublished = (instrument, _) => RefreshInstrument(instrument);
                tempListener.Start();
            }
        }

        // Called under _instrument lock
        private void RefreshInstrument(Instrument instrument)
        {
            var alreadyEnabled = _instruments.TryGetValue(instrument, out var state);
            var enable = false;
            var rule = GetMostSpecificRule(instrument);
            if (rule != null)
            {
                enable = rule.Enable;
            }

            if (!enable && alreadyEnabled)
            {
                _instruments.Remove(instrument);
                _metricsListener.MeasurementsCompleted(instrument, state);
                _meterListener.DisableMeasurementEvents(instrument);
            }
            else if (enable && !alreadyEnabled)
            {
                // The listener gets a chance to decline the instrument.
                if (_metricsListener.InstrumentPublished(instrument, out state))
                {
                    _instruments.Add(instrument, state);
                    _meterListener.EnableMeasurementEvents(instrument, state);
                }
            }
        }

        private InstrumentRule? GetMostSpecificRule(Instrument instrument)
        {
            InstrumentRule? best = null;
            foreach (var rule in _rules)
            {
                if (RuleMatches(rule, instrument, _metricsListener.Name, _meterFactory)
                    && IsMoreSpecific(rule, best, isLocalScope: instrument.Meter.Scope == _meterFactory))
                {
                    best = rule;
                }
            }

            return best;
        }

        // internal for testing
        internal static bool RuleMatches(InstrumentRule rule, Instrument instrument, string listenerName, IMeterFactory meterFactory)
        {
            // Exact match or empty
            if (!string.IsNullOrEmpty(rule.ListenerName)
                && !string.Equals(rule.ListenerName, listenerName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Exact match or empty
            if (!string.IsNullOrEmpty(rule.InstrumentName)
                && !string.Equals(rule.InstrumentName, instrument.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!(rule.Scopes.HasFlag(MeterScope.Global) && instrument.Meter.Scope == null)
                && !(rule.Scopes.HasFlag(MeterScope.Local) && instrument.Meter.Scope == meterFactory))
            {
                return false;
            }

            // Meter

            var ruleMeterName = rule.MeterName.AsSpan();
            // Don't allow "*" anywhere except at the end.
            var starIndex = ruleMeterName.IndexOf('*');
            if (starIndex != -1 && starIndex != ruleMeterName.Length - 1)
            {
                return false;
            }
            // Rule "System.Net.*" matches meter "System.Net" and "System.Net.Http"
            if (ruleMeterName.EndsWith(".*".AsSpan(), StringComparison.Ordinal))
            {
                ruleMeterName = ruleMeterName.Slice(0, ruleMeterName.Length - 2);
            }
            // System.Net* matches System.Net and System.Net.Http
            else if (starIndex != -1)
            {
                ruleMeterName = ruleMeterName.Slice(0, ruleMeterName.Length - 1);
            }

            // Rule "" matches everything
            if (ruleMeterName.IsEmpty)
            {
                return true;
            }

            // "System.Net" matches "System.Net" and "System.Net.Http"
            return instrument.Meter.Name.AsSpan().StartsWith(ruleMeterName, StringComparison.OrdinalIgnoreCase)
                // Exact match +/- ".*"
                && (ruleMeterName.Length == instrument.Meter.Name.Length
                    // Only allow StartsWith on segment boundaries
                    || instrument.Meter.Name[ruleMeterName.Length] == '.');
        }

        // Everything must already match the Instrument and listener, or be blank.
        // internal for testing
        internal static bool IsMoreSpecific(InstrumentRule rule, InstrumentRule? best, bool isLocalScope)
        {
            if (best == null)
            {
                return true;
            }

            // Listener name
            if (!string.IsNullOrEmpty(rule.ListenerName) && string.IsNullOrEmpty(best.ListenerName))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(rule.ListenerName) && !string.IsNullOrEmpty(best.ListenerName))
            {
                return false;
            }

            // Meter name
            if (!string.IsNullOrEmpty(rule.MeterName))
            {
                if (string.IsNullOrEmpty(best.MeterName))
                {
                    return true;
                }

                // Longer is more specific.
                if (rule.MeterName.Length != best.MeterName.Length)
                {
                    return rule.MeterName.Length > best.MeterName.Length;
                }
            }
            else if (!string.IsNullOrEmpty(best.MeterName))
            {
                return false;
            }

            // Instrument name
            if (!string.IsNullOrEmpty(rule.InstrumentName) && string.IsNullOrEmpty(best.InstrumentName))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(rule.InstrumentName) && !string.IsNullOrEmpty(best.InstrumentName))
            {
                return false;
            }

            // Scope

            // Already matched as local
            if (isLocalScope)
            {
                // Local is more specific than Local+Global
                if (!rule.Scopes.HasFlag(MeterScope.Global) && best.Scopes.HasFlag(MeterScope.Global))
                {
                    return true;
                }
                else if (rule.Scopes.HasFlag(MeterScope.Global) && !best.Scopes.HasFlag(MeterScope.Global))
                {
                    return false;
                }
            }
            // Already matched as global
            else
            {
                // Global is more specific than Local+Global
                if (!rule.Scopes.HasFlag(MeterScope.Local) && best.Scopes.HasFlag(MeterScope.Local))
                {
                    return true;
                }
                else if (rule.Scopes.HasFlag(MeterScope.Local) && !best.Scopes.HasFlag(MeterScope.Local))
                {
                    return false;
                }
            }

            // All things being equal, take the last one.
            return true;
        }

        public void RecordObservableInstruments() => _meterListener.RecordObservableInstruments();

        public void Dispose()
        {
            _disposed = true;
            _meterListener.Dispose();
        }
    }
}
