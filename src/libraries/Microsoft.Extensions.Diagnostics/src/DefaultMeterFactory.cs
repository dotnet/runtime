// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Metrics
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

            Debug.Assert(options.Name != null);

            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (_cachedMeters.TryGetValue(options.Name, out List<Meter>? meterList))
                {
                    foreach (Meter meter in meterList)
                    {
                        if (meter.Version == options.Version && CompareTags(meter.Tags, options.Tags))
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

        private static bool CompareTags(IEnumerable<KeyValuePair<string, object?>>? tags1, IEnumerable<KeyValuePair<string, object?>>? tags2)
        {
            if (tags1 is null)
            {
                return tags2 is null;
            }

            if (tags2 is null)
            {
                return false;
            }

            if (tags1 is ICollection<KeyValuePair<string, object?>> firstCol && tags2 is ICollection<KeyValuePair<string, object?>> secondCol)
            {
                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<KeyValuePair<string, object?>> firstList && secondCol is IList<KeyValuePair<string, object?>> secondList)
                {
                    int count = firstList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        KeyValuePair<string, object?> pair1 = firstList[i];
                        KeyValuePair<string, object?> pair2 = secondList[i];
                        if (pair1.Key != pair2.Key || !object.Equals(pair1.Value, pair2.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<KeyValuePair<string, object?>> e1 = tags1.GetEnumerator())
            using (IEnumerator<KeyValuePair<string, object?>> e2 = tags2.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    KeyValuePair<string, object?> pair1 = e1.Current;
                    if (!e2.MoveNext() || pair1.Key != e2.Current.Key || !object.Equals(pair1.Value, e2.Current.Value))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }
    }
}
