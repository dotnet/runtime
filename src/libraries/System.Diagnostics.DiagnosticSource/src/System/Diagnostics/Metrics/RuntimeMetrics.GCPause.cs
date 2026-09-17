// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Diagnostics.Metrics;

internal static partial class RuntimeMetrics
{
    private static void InitializeGCPauseMetrics() => _ = new GCPauseMetrics(s_meter);

    private sealed class GCPauseMetrics
    {
        private readonly object _lock = new();
        private readonly Histogram<double> _histogram;
        private Subscription? _subscription;
        private long _epoch;
        private bool _updating;

        internal GCPauseMetrics(Meter meter)
        {
            _histogram = meter.CreateHistogram<double>(
                "dotnet.gc.pause.duration",
                unit: "s",
                description: "The duration of each GC-accounted pause contribution. Measurements are delivered asynchronously through EventPipe while listeners are enabled. No measurements are emitted when the runtime does not provide these events. Buffer overflow and session reconfiguration can drop measurements; loss counts are unavailable.");
            _histogram.SetMeasurementStateCallback(SubscriptionsChanged);
        }

        private void SubscriptionsChanged()
        {
            lock (_lock)
            {
                if (_updating)
                {
                    return;
                }

                _updating = true;
            }

            // EventListener operations can call user code. Serialize updates without holding our lock
            // across construction, enabling, or disposal, and reconcile any reentrant state changes.
            bool completed = false;
            try
            {
                while (true)
                {
                    bool enabled;
                    long epoch;
                    lock (_lock)
                    {
                        lock (Instrument.SyncObject)
                        {
                            enabled = _histogram.Enabled && !_histogram.Meter.Disposed;
                            epoch = _histogram.MeasurementEpoch;
                        }

                        if (_epoch == epoch && (_subscription is not null) == enabled)
                        {
                            _updating = false;
                            completed = true;
                            return;
                        }

                        _epoch = epoch;
                    }

                    _subscription?.Dispose();
                    _subscription = null;
                    if (enabled)
                    {
                        _subscription = new Subscription(_histogram, epoch);
                        _subscription.Start();
                    }
                }
            }
            finally
            {
                if (!completed)
                {
                    try
                    {
                        _subscription?.Dispose();
                    }
                    finally
                    {
                        _subscription = null;
                        lock (_lock)
                        {
                            _updating = false;
                        }
                    }
                }
            }
        }

        private sealed class Subscription(Histogram<double> histogram, long epoch) : EventListener
        {
            private const string RuntimeProviderName = "Microsoft-Windows-DotNETRuntime";
            private const int GCPauseEventId = 304;
            private const EventKeywords GCPauseKeyword = (EventKeywords)0x04000000;

            private EventSource? _eventSource;
            private bool _active;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                // This callback runs in the base constructor, before histogram state is available.
                if (eventSource.Name == RuntimeProviderName)
                {
                    _eventSource = eventSource;
                }
            }

            internal void Start()
            {
                // The runtime provider is absent when EventSource support is disabled.
                if (_eventSource is not null)
                {
                    Volatile.Write(ref _active, true);
                    EnableEvents(_eventSource, EventLevel.Informational, GCPauseKeyword);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId != GCPauseEventId || eventData.Version != 1 ||
                    !Volatile.Read(ref _active) || histogram.MeasurementEpoch != epoch)
                {
                    return;
                }

                if (eventData.Payload is [ulong, ulong durationMicroseconds, uint generation, uint kind, ushort] &&
                    generation <= GC.MaxGeneration && kind <= 1 && (kind == 0 || generation == 2))
                {
                    histogram.Record(durationMicroseconds / 1_000_000d,
                        new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[generation]),
                        new KeyValuePair<string, object?>("gc.pause.type", kind == 0 ? "blocking" : "background"));
                }
                else
                {
                    Debug.Fail("Unexpected GC pause event payload.");
                }
            }

            public override void Dispose()
            {
                Volatile.Write(ref _active, false);
                base.Dispose();
            }
        }
    }
}
