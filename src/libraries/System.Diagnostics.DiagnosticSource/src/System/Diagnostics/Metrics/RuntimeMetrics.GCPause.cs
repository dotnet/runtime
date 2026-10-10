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
        private long _epoch = -1;

        internal GCPauseMetrics(Meter meter)
        {
            _histogram = meter.CreateHistogram<double>(
                "dotnet.gc.pause.duration",
                unit: "s",
                description: "The duration of each GC-accounted pause contribution. Collection starts asynchronously after listeners are enabled, and measurements are delivered asynchronously through EventPipe. No measurements are emitted when the runtime does not provide these events. Buffer overflow and session reconfiguration can drop measurements; loss counts are unavailable.");
            _histogram.SetMeasurementStateCallback(SubscriptionsChanged);
        }

        private void SubscriptionsChanged(Instrument instrument, bool enabled, long epoch) =>
            ThreadPool.UnsafeQueueUserWorkItem(
                static state => state.Producer.UpdateSubscription(state.Instrument, state.Enabled, state.Epoch),
                (Producer: this, Instrument: instrument, Enabled: enabled, Epoch: epoch), preferLocal: false);

        private void UpdateSubscription(Instrument instrument, bool enabled, long epoch)
        {
            Exception? failure = null;
            lock (_lock)
            {
                if (epoch <= _epoch || epoch != instrument.MeasurementEpoch)
                {
                    return;
                }

                // Only workers take this lock. EventListener callbacks can enqueue more work,
                // but never synchronously wait for it or construct another listener.
                try
                {
                    _subscription?.Dispose();
                    _subscription = null;
                    if (enabled)
                    {
                        _subscription = new Subscription(_histogram, epoch);
                        _subscription.Start();
                    }

                    _epoch = epoch;
                }
                catch (Exception e) when (e is EventSourceException or InvalidOperationException)
                {
                    _subscription?.Dispose();
                    _subscription = null;
                    failure = e;
                }
            }

            if (failure is not null)
            {
                MetricsEventSource.Log.Message(failure.ToString());
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
                if (eventData.EventId != GCPauseEventId || eventData.Version != 0 ||
                    !Volatile.Read(ref _active) || histogram.MeasurementEpoch != epoch)
                {
                    return;
                }

                if (eventData.Payload is [uint, ulong durationMicroseconds, uint generation, uint type, ushort] &&
                    generation <= GC.MaxGeneration && type <= 2 && (type != 1 || generation == 2))
                {
                    histogram.Record(durationMicroseconds / 1_000_000d,
                        new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[generation]),
                        new KeyValuePair<string, object?>("gc.pause.type", type == 1 ? "background" : "blocking"));
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
