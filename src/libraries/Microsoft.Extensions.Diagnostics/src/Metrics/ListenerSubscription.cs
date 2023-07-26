// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal class ListenerSubscription : IMetricsSource, IDisposable
    {
        private readonly MeterListener _meterListener;
        private readonly IMetricsListener _metricsListener;
        private readonly HashSet<Instrument> _instruments = new();
        private readonly HashSet<Instrument> _enabled = new();
        private IList<InstrumentEnableRule> _rules = Array.Empty<InstrumentEnableRule>();
        private bool _disposed;

        internal ListenerSubscription(IMetricsListener metricsListener)
        {
            _metricsListener = metricsListener;

            _meterListener = new MeterListener();
        }

        public void Start()
        {
            _meterListener.InstrumentPublished = InstrumentPublished;
            _meterListener.MeasurementsCompleted = MeasurementsCompleted;
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<byte>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<short>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<int>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<long>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<float>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<double>());
            _meterListener.SetMeasurementEventCallback(_metricsListener.GetMeasurementHandler<decimal>());
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

                if (_instruments.Contains(instrument))
                {
                    Debug.Assert(false, "InstrumentPublished called twice for the same instrument");
                    return;
                }

                _instruments.Add(instrument);
                RefreshInstrument(instrument);
            }
        }

        private void MeasurementsCompleted(Instrument instrument, object? state)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                if (_instruments.Remove(instrument))
                {
                    if (_enabled.Remove(instrument))
                    {
                        _meterListener.DisableMeasurementEvents(instrument);
                    }
                }
                else
                {
                    Debug.Assert(false, "InstrumentPublished was not called for this instrument");
                }
            }
        }

        internal void UpdateRules(IList<InstrumentEnableRule> rules)
        {
            lock (_instruments)
            {
                if (_disposed)
                {
                    return;
                }

                _rules = rules;

                foreach (var instrument in _instruments)
                {
                    RefreshInstrument(instrument);
                }
            }
        }

        // Called under _instrument lock
        private void RefreshInstrument(Instrument instrument)
        {
            var alreadyEnabled = _enabled.Contains(instrument);
            var enable = false;
            var rule = GetMostSpecificRule(instrument);
            if (rule != null)
            {
                enable = rule.Enable;
            }

            if (!enable && alreadyEnabled)
            {
                _enabled.Remove(instrument);
                _meterListener.DisableMeasurementEvents(instrument);
            }
            else if (enable && !alreadyEnabled)
            {
                _meterListener.EnableMeasurementEvents(instrument);
                _enabled.Add(instrument);
            }
        }

        private InstrumentEnableRule? GetMostSpecificRule(Instrument instrument)
        {
            InstrumentEnableRule? best = null;
            foreach (var rule in _rules)
            {
                if (RuleMatches(rule, instrument) && IsMoreSpecific(rule, best))
                {
                    best = rule;
                }
            }

            return best;
        }

        private bool RuleMatches(InstrumentEnableRule rule, Instrument instrument)
        {
            // Exact match or empty
            if (!string.IsNullOrEmpty(rule.ListenerName)
                && !string.Equals(rule.ListenerName, _metricsListener.Name, StringComparison.OrdinalIgnoreCase))
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

            if (!string.IsNullOrEmpty(rule.MeterName))
            {
                // TODO: StartsWith. E.g. "System.Net.Http" or "System.Net.Http.*" should match "System.Net.Http.SocketsHttpHandler" and "System.Net.Http.HttpClient".
                if (!string.Equals(rule.MeterName, instrument.Meter.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        // Everything must already match the Instrument and listener, or be blank.
        // Which rule has more non-blank fields? Or longer Meter name?
        //
        private static bool IsMoreSpecific(InstrumentEnableRule rule, InstrumentEnableRule? best)
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

            // Scopes TODO:??

            return false;
        }

        public void RecordObservableInstruments()
        {
            foreach (var instrument in _enabled)
            {
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
            _meterListener.Dispose();
        }
    }
}
