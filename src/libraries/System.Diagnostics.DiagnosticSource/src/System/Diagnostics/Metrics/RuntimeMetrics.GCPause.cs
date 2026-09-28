// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics.Metrics;

internal static partial class RuntimeMetrics
{
    private const string GCPauseReportingTypeName = "System.GCPauseReporting, System.Private.CoreLib";

    private static void InitializeGCPauseMetrics()
    {
        // A newer package may run with a CoreLib that predates this internal bridge.
        if (Type.GetType(GCPauseReportingTypeName, throwOnError: false) is null || !IsGCPauseReportingSupported(null))
        {
            return;
        }

        _ = new GCPauseMetrics(s_meter);
        s_meter.CreateObservableCounter(
            "dotnet.gc.pause.dropped",
            () => GetDroppedGCPauseCount(null),
            unit: "{measurement}",
            description: "The number of GC pause measurements dropped because the reporting buffer was full since the process has started.");
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "IsSupported")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static extern bool IsGCPauseReportingSupported([UnsafeAccessorType(GCPauseReportingTypeName)] object? _);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Register")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static extern IDisposable RegisterGCPauseCallback(
        [UnsafeAccessorType(GCPauseReportingTypeName)] object? _, Action<ulong, ulong, int, int> callback);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetDroppedCount")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static extern long GetDroppedGCPauseCount([UnsafeAccessorType(GCPauseReportingTypeName)] object? _);

    private sealed class GCPauseMetrics
    {
        private readonly object _lock = new();
        private readonly Histogram<double> _histogram;
        private Subscription? _subscription;
        private long _epoch;

        internal GCPauseMetrics(Meter meter)
        {
            _histogram = meter.CreateHistogram<double>(
                "dotnet.gc.pause.duration",
                unit: "s",
                description: "The duration of each GC-accounted pause contribution. Measurements are delivered asynchronously on a background thread while listeners are enabled; callbacks must not throw. Buffer overflow can drop measurements.");
            _histogram.SetMeasurementStateCallback(SubscriptionsChanged);
        }

        private void SubscriptionsChanged()
        {
            lock (_lock)
            {
                bool enabled;
                long epoch;
                lock (Instrument.SyncObject)
                {
                    enabled = _histogram.Enabled && !_histogram.Meter.Disposed;
                    epoch = _histogram.MeasurementEpoch;
                }

                if (_epoch == epoch && (_subscription is not null) == enabled)
                {
                    return;
                }

                _epoch = epoch;
                if (_subscription is not null)
                {
                    Subscription previous = _subscription;
                    _subscription = null;
                    Volatile.Write(ref previous._active, false);
                    previous._registration?.Dispose();
                }

                if (enabled)
                {
                    var subscription = new Subscription(_histogram, epoch);
                    subscription._registration = RegisterGCPauseCallback(null, subscription.Record);
                    _subscription = subscription;
                }
            }
        }

        private sealed class Subscription(Histogram<double> histogram, long epoch)
        {
            internal bool _active = true;
            internal IDisposable? _registration;

            internal void Record(ulong durationMicroseconds, ulong _, int generation, int kind)
            {
                if (!Volatile.Read(ref _active) || histogram.MeasurementEpoch != epoch)
                {
                    return;
                }

                Debug.Assert((uint)generation <= GC.MaxGeneration);
                Debug.Assert((uint)kind <= 1);
                Debug.Assert(kind == 0 || generation == 2);
                histogram.Record(durationMicroseconds / 1_000_000d,
                    new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[generation]),
                    new KeyValuePair<string, object?>("gc.pause.type", kind == 0 ? "blocking" : "background"));
            }
        }
    }
}
