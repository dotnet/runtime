// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class DefaultMeterFactory : IMeterFactory
    {
        private readonly Dictionary<string, List<Meter>> _cachedMeters = new();
        private bool _disposed;

        public DefaultMeterFactory() { }

        public Meter Create(MeterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Debug.Assert(options.Name is not null);

            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMeterFactory));
                }

                if (_cachedMeters.TryGetValue(options.Name, out List<Meter>? meterList))
                {
                    foreach (Meter meter in meterList)
                    {
                        if (meter.Version == options.Version && DiagnosticsHelper.CompareTags(meter.Tags, options.Tags))
                        {
                            return meter;
                        }
                    }
                }
                else
                {
                    meterList = new List<Meter>();
                    _cachedMeters.Add(options.Name, meterList);
                }

                Meter m = new Meter(options.Name, options.Version, options.Tags, scope: this);
                meterList.Add(m);
                return m;
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

                foreach (List<Meter> meterList in _cachedMeters.Values)
                {
                    foreach (Meter meter in meterList)
                    {
                        meter.Dispose();
                    }
                }

                _cachedMeters.Clear();
            }
        }
    }
}
