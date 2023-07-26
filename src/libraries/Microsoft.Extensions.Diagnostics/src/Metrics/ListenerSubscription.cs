// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
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
        private readonly HashSet<Instrument> _listening = new();
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
                RefreshConnection(instrument);
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
                    if (_listening.Remove(instrument))
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
                    RefreshConnection(instrument);
                }
            }
        }

        // Called under _instrument lock
        private void RefreshConnection(Instrument instrument)
        {
            var alreadyListening = _listening.Contains(instrument);
            var listen = false;
            foreach (var rule in _rules)
            {
                // TODO: Most specific match
                if (RuleMatches(rule, instrument))
                {
                    listen = true;
                    break;
                }
            }

            // Remove any that are no longer needed
            if (!listen && alreadyListening)
            {
                _listening.Remove(instrument);
                _meterListener.DisableMeasurementEvents(instrument);
            }
            else if (listen && !alreadyListening)
            {
                _meterListener.EnableMeasurementEvents(instrument);
                _listening.Add(instrument);
            }
        }

        private bool RuleMatches(InstrumentEnableRule rule, Instrument instrument)
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

            if (!(rule.Scopes.HasFlag(MeterScope.Global) && instrument.Meter.Scope == null)
                && !(rule.Scopes.HasFlag(MeterScope.Local) && instrument.Meter.Scope != null)) // TODO: What should we be comparing Scope to, the DefaultMeterFactory / IMeterFactory?
            {
                return false;
            }

            /* TODO: Remove filters
            if (rule.Filter != null)
            {
                return rule.Filter(listener.Name, instrument);
            }
            */

            if (string.IsNullOrEmpty(rule.ListenerName))
            {
                return true;
            }

            return string.Equals(rule.ListenerName, _metricsListener.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.ListenerName, _metricsListener.GetType().FullName, StringComparison.OrdinalIgnoreCase);
        }

        public void RecordObservableInstruments()
        {
            foreach (var instrument in _listening)
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
