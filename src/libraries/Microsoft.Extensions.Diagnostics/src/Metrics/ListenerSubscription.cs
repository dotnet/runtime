// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
#if NET
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal class ListenerSubscription : IObservableInstrumentsSource, IDisposable
    {
        private readonly MeterListener _meterListener;
        private readonly IMetricsListener _metricsListener;
#if NET
        private readonly ConditionalWeakTable<Instrument, InstrumentStatus> _instruments = new();
#else
        // TODO: Can't enumerate ConditionalWeakTable in .NET Standard 2.0. How do we clean up the instruments?
        private readonly Dictionary<Instrument, InstrumentStatus> _instruments = new();
#endif
        private IList<InstrumentRule> _rules = Array.Empty<InstrumentRule>();
        private bool _disposed;

        internal ListenerSubscription(IMetricsListener metricsListener)
        {
            _metricsListener = metricsListener;

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
            _meterListener.Start();
            _metricsListener.Initialize(this);
        }

        private void InstrumentPublished(Instrument instrument, MeterListener _)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                if (_instruments.TryGetValue(instrument, out var _))
                {
                    Debug.Assert(false, "InstrumentPublished called twice for the same instrument");
                    return;
                }

                var status = new InstrumentStatus();
                _instruments.Add(instrument, status);
                RefreshInstrument(instrument, status);
            }
        }

        // Called when we call DisableMeasurementEvents, like when a rule is disabled.
        private void MeasurementsCompleted(Instrument instrument, object? state)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                if (!_instruments.TryGetValue(instrument, out var status))
                {
                    Debug.Assert(false, "MeasurementsCompleted called for an instrument that was never published");
                    return;
                }
                status.Enabled = false;

                if (status.Published)
                {
                    status.Published = false;
                    if (status.ListenerEnabled)
                    {
                        _metricsListener.MeasurementsCompleted(instrument, status.State);
                    }
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

                foreach (var instrumentPair in _instruments)
                {
                    RefreshInstrument(instrumentPair.Key, instrumentPair.Value);
                }
            }
        }

        // Called under _instrument lock
        private void RefreshInstrument(Instrument instrument, InstrumentStatus status)
        {
            var alreadyEnabled = status.Enabled;
            var enable = false;
            var rule = GetMostSpecificRule(instrument);
            if (rule != null)
            {
                enable = rule.Enable;
            }

            if (!enable && alreadyEnabled)
            {
                status.Enabled = false;
                _meterListener.DisableMeasurementEvents(instrument);
            }
            else if (enable && !alreadyEnabled)
            {
                // The first time we enable an instrument, we need to call InstrumentPublished.
                if (!status.Published)
                {
                    // However, a listener might decline to enable the instrument, remember that.
                    status.Published = true;
                    status.ListenerEnabled = _metricsListener.InstrumentPublished(instrument, out var state);
                    status.State = state;
                }

                if (status.ListenerEnabled)
                {
                    _meterListener.EnableMeasurementEvents(instrument, status.State);
                    status.Enabled = true;
                }
            }
        }

        private InstrumentRule? GetMostSpecificRule(Instrument instrument)
        {
            InstrumentRule? best = null;
            foreach (var rule in _rules)
            {
                if (RuleMatches(rule, instrument, _metricsListener.Name)
                    && IsMoreSpecific(rule, best, isLocalScope: instrument.Meter.Scope != null))
                {
                    best = rule;
                }
            }

            return best;
        }

        // internal for testing
        internal static bool RuleMatches(InstrumentRule rule, Instrument instrument, string listenerName)
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
                && !(rule.Scopes.HasFlag(MeterScope.Local) && instrument.Meter.Scope != null)) // TODO: What should we be comparing Scope to, the DefaultMeterFactory / IMeterFactory?
            {
                return false;
            }

            // Meter

            var ruleMeterName = rule.MeterName.AsSpan();
            // Rule "System.Net.*" matches meter "System.Net" and "System.Net.Http"
            if (ruleMeterName.EndsWith(".*".AsSpan(), StringComparison.Ordinal))
            {
                ruleMeterName = ruleMeterName.Slice(0, ruleMeterName.Length - 2);
            }

            // Rule "" or "*" matches everything
            if (ruleMeterName.IsEmpty || ruleMeterName.Equals("*".AsSpan(), StringComparison.Ordinal))
            {
                return true;
            }

            // Rule "System.Net.Http" doesn't match meter "System.Net"
            if (ruleMeterName.Length > instrument.Meter.Name.Length)
            {
                return false;
            }

            // Exact match +/- ".*"
            if (ruleMeterName.Equals(instrument.Meter.Name.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Rule "System.Data" doesn't match meter "System.Net"
            if (!instrument.Meter.Name.AsSpan().StartsWith(ruleMeterName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Only allow StartsWith on segment boundaries
            return instrument.Meter.Name[ruleMeterName.Length] == '.';
        }

        // Everything must already match the Instrument and listener, or be blank.
        // internal for testing
        internal static bool IsMoreSpecific(InstrumentRule rule, InstrumentRule? best, bool isLocalScope)
        {
            if (best == null)
            {
                return true;
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

            // Listener name
            if (!string.IsNullOrEmpty(rule.ListenerName) && string.IsNullOrEmpty(best.ListenerName))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(rule.ListenerName) && !string.IsNullOrEmpty(best.ListenerName))
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

        private class InstrumentStatus
        {
            public object? State { get; set; }
            public bool Published { get; set; }
            public bool Enabled { get; set; }
            public bool ListenerEnabled { get; set; }
        }
    }
}
