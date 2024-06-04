// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class MetricsSubscriptionManager : IDisposable
    {
        private readonly ListenerSubscription[] _listeners;
        private readonly IDisposable? _changeTokenRegistration;
        private bool _disposed;

        public MetricsSubscriptionManager(IEnumerable<IMetricsListener> listeners, IOptionsMonitor<MetricsOptions> options, IMeterFactory meterFactory)
        {
            var list = listeners.ToList();
            _listeners = new ListenerSubscription[list.Count];
            for (int i = 0; i < _listeners.Length; i++)
            {
                _listeners[i] = new ListenerSubscription(list[i], meterFactory);
            }
            _changeTokenRegistration = options.OnChange(UpdateRules);
            UpdateRules(options.CurrentValue);
        }

        public void Initialize()
        {
            foreach (var listener in _listeners)
            {
                listener.Initialize();
            }
        }

        private void UpdateRules(MetricsOptions options)
        {
            if (_disposed)
            {
                return;
            }
            var rules = options.Rules;

            foreach (var listener in _listeners)
            {
                listener.UpdateRules(rules);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _changeTokenRegistration?.Dispose();
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }
    }
}
